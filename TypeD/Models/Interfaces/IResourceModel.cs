using System;
using System.Collections.Generic;

namespace TypeD.Models.Interfaces
{
    public interface IResourceModel : IModel
    {
        public void Add(List<object> values);
        public void Add(object value);
        public void Add(string key, object value);
        public void Add(List<Tuple<string, object>> keyValues);
        public void Remove(string key);
        public T Get<T>(string key) where T : class;
        public T Get<T>() where T : class;
    }
}
