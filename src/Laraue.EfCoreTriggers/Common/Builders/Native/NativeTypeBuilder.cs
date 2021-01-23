using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Laraue.EfCoreTriggers.Common.Builders.Native
{
    public abstract class NativeTypeBuilder
    {
        IDictionary<string, string> _tokens;
        string _nameToken;
        public string AnnotationName { get; }
        public string NativeObjectName { get; }
        public string Sql { get; }
        public int Order { get; }

        public NativeTypeBuilder(string annotationPrefix, string nameToken, string name, string rawScript, int order)
        {
            _nameToken = nameToken;
            Sql = rawScript;
            Order = order;

            AnnotationName = $"{string.Format(annotationPrefix, order)}{name}";
            NativeObjectName = name;
        }

        protected void SetTokens(IDictionary<string, string> tokens)
        {
            _tokens = tokens;
        }

        public IDictionary<string, string> Tokens
        {
            get
            {
                return
                    new Dictionary<string, string>(
                        new Dictionary<string, string>()
                        {
                            { _nameToken, NativeObjectName }
                        }.Union(_tokens ?? new Dictionary<string, string>()));
            }
        }
    }
}
