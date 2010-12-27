﻿using Lucene.Net.Documents;

namespace Core.Abstractions
{
    public interface IConverter
    {
        IItem FromDocumentToItem(Document document);
    }

    public interface IConverter<in T> : IConverter
    {
        string ToId(T t);
        Document ToDocument(T t);
        string ToName(T t);
        string ToType(T t);
    }

    public static class IConverterExtensions
    {
        public static string GetId(this IConverter self)
        {
            return self.GetType().FullName;
        }
    }
}