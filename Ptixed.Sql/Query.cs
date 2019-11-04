using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ptixed.Sql.Impl;
using Ptixed.Sql.Meta;
using Ptixed.Sql.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Ptixed.Sql
{
    public class Query
    {
        private class Part
        {
            public readonly string Text;
            public readonly object Parameter;
            public readonly bool IsParameter;

            public Part(string text)
            {
                Text = text;
                IsParameter = false;
            }

            public Part(object parameter)
            {
                Parameter = parameter;
                IsParameter = true;
            }

            public override string ToString()
            {
                if (IsParameter)
                    return "P " + Parameter;
                return "T " + Text;
            }
        }

        private static readonly ConcurrentDictionary<string, List<Func<Part[][], Part[]>>> Cache = new ConcurrentDictionary<string, List<Func<Part[][], Part[]>>>();

        private readonly List<Part> _parts =  new List<Part>();
        public bool IsEmpty => _parts.Count == 0;

        public Query() { }
        public Query(string text) => Append(text);
        public Query(Expression<Func<FormattableString>> expression) => Append(expression);
        public Query(object parameter) => Append(parameter);

        public Query Append(string text)
        {
            _parts.Add(new Part(text));
            return this;
        }   

        public Query Append(Expression<Func<FormattableString>> expression)
        {
            var body = (MethodCallExpression)expression.Body;
            var format = (string)((ConstantExpression)body.Arguments[0]).Value;

            var parts = Cache.GetOrAdd(format, f =>
            {
                var tree = (InterpolatedStringExpressionSyntax)SyntaxFactory.ParseExpression("$\"" + format.Replace("\"", "\\\"") + '"');
                var ret = new List<Func<Part[][], Part[]>>(tree.Contents.Count);
                foreach (var i in tree.Contents)
                    switch (i)
                    {
                        case InterpolatedStringTextSyntax i0:
                            var text = new[] { new Part(i0.TextToken.Text) };
                            ret.Add(_ => text);
                            break;
                        case InterpolationSyntax i1 when i1.Expression is LiteralExpressionSyntax i11:
                            if (i1.AlignmentClause != null || i1.FormatClause != null)
                                throw PtixedException.InvalidExpression(i1);
                            ret.Add(parameters => parameters[(int)i11.Token.Value]);
                            break;
                        default:
                            throw PtixedException.InvalidExpression(i);
                    }
                return ret;
            });
             
            var ps = ((NewArrayExpression)body.Arguments[1]).Expressions.Select(x => FormatExpression(x).ToArray()).ToArray();

            _parts.AddRange(parts.SelectMany(x => x(ps)));
            return this;
        }   
        
        public Query Append(object parameter)
        {
            _parts.Add(new Part(parameter));
            return this;
        }
        
        public Query Append(Query query)
        {
            _parts.AddRange(query._parts);
            return this;
        }  
        
        public static Query Join(string separator, IEnumerable<Query> parts)
        {
            var query = new Query();
            using (var e = parts.GetEnumerator())
                if (e.MoveNext())
                {
                    query.Append(e.Current);
                    while(e.MoveNext())
                        query.Append(separator).Append(e.Current);
                }
            return query;
        }
        
        public override string ToString() => ToSql(new SqlCommand(), new MappingConfig()).CommandText;

        public SqlCommand ToSql(SqlCommand command, MappingConfig mapping)
        {
            var sb = new StringBuilder();
            var i = 0;
            foreach (var part in _parts)
                if (part.IsParameter)
                {
                    sb.Append('@').Append(i);

                    var value = mapping.ToDb(part.Parameter?.GetType(), part.Parameter);
                    var parameter = value as SqlParameter ?? new SqlParameter { Value = value };
                    parameter.ParameterName = i.ToString();
                    command.Parameters.Add(parameter);

                    i++;
                }
                else
                    sb.Append(part.Text);

            command.CommandText = sb.ToString();

            return command;
        }

        private IEnumerable<Part> FormatExpression(Expression expression)
        {
            switch (expression)
            {
                case ConstantExpression ce:
                    return FormatValue(ce.Value);
                case MemberExpression me:
                    return FormatMember(me);
                case UnaryExpression ue:
                    return FormatExpression(ue.Operand);
                default:
                    throw PtixedException.InvalidExpression(expression);
            }
        }

        private IEnumerable<Part> FormatMember(MemberExpression expr)
        {
            var owner = Reflection.Execute(expr.Expression);
            var value = Reflection.GetValue(expr.Member, owner);
            return FormatValue(value);
        }

        private IEnumerable<Part> FormatValue(object value)
        {
            switch (value)
            {
                case null:
                    yield return new Part("NULL");
                    break;
                case Query q:
                    foreach (var part in q._parts)
                        yield return part;
                    break;
                case Table tm:
                    yield return new Part(tm.ToString());
                    break;
                case PhysicalColumn pc:
                    yield return new Part(pc.ToString());
                    break;
                case ColumnValue cv:
                    yield return new Part(cv.ToString());
                    break;
                case int i:
                    yield return new Part(i.ToString());
                    break;
                case IEnumerable<object> ie when !ie.Any():
                    yield return new Part("(SELECT 0 WHERE 1 = 0)");
                    break;
                case IEnumerable<object> ie:
                    using (var enumerator = ie.GetEnumerator())
                    {
                        enumerator.MoveNext();
                        yield return new Part(enumerator.Current);
                        while (enumerator.MoveNext())
                        {
                            yield return new Part(", ");
                            yield return new Part(enumerator.Current);
                        }
                    }
                    break;
                default:
                    yield return new Part(value);
                    break;
            }
        }
    }
}
