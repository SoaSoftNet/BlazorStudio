using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorStudio.Shared.FileSystem.Classes;
using Fluxor;
using PlainTextEditor.ClassLib.Sequence;

namespace PlainTextEditor.ClassLib.Store.PlainTextEditorCase;

public partial record PlainTextEditorStates
{
    private record PlainTextEditorRecord(PlainTextEditorKey PlainTextEditorKey,
            SequenceKey SequenceKey,
            ImmutableList<IPlainTextEditorRow> List,
            int CurrentRowIndex,
            int CurrentTokenIndex,
            IFileCoordinateGrid? FileCoordinateGrid,
            RichTextEditorOptions RichTextEditorOptions,
            bool UseCarriageReturnNewLine = false)
        : IPlainTextEditor
    {
        public PlainTextEditorRecord(PlainTextEditorKey plainTextEditorKey) : this(plainTextEditorKey,
            SequenceKey.NewSequenceKey(),
            ImmutableList<IPlainTextEditorRow>.Empty,
            CurrentRowIndex: 0,
            CurrentTokenIndex: 0,
            null,
            new RichTextEditorOptions())
        {
            var startingRow = new PlainTextEditorRow(null);

            List = List.Add(startingRow);
        }

        public IPlainTextEditorRow CurrentPlainTextEditorRow => List[CurrentRowIndex];

        public TextTokenKey CurrentTextTokenKey => CurrentPlainTextEditorRow.List[CurrentTokenIndex].Key;
        public ITextToken CurrentTextToken => CurrentPlainTextEditorRow.List[CurrentTokenIndex];

        public T GetCurrentTextTokenAs<T>()
            where T : class
        {
            return CurrentTextToken as T
                   ?? throw new ApplicationException($"Expected {typeof(T).Name}");
        }

        /// <summary>
        /// TODO: Remove this and in its place use <see cref="ConvertIPlainTextEditorRowAs"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public T GetCurrentPlainTextEditorRowAs<T>()
            where T : class
        {
            return CurrentPlainTextEditorRow as T
                   ?? throw new ApplicationException($"Expected {typeof(T).Name}");
        }

        public T ConvertIPlainTextEditorRowAs<T>(IPlainTextEditorRow plainTextEditorRow)
            where T : class
        {
            return plainTextEditorRow as T
                   ?? throw new ApplicationException($"Expected {typeof(T).Name}");
        }

        public string GetPlainText()
        {
            var builder = new StringBuilder();

            foreach (var row in List)
            {
                foreach (var token in row.List)
                {
                    if (token.Key == List[0].List[0].Key)
                    {
                        // Is first start of row so skip
                        // as it would insert a enter key stroke at start
                        // of document otherwise.

                        continue;
                    }

                    builder.Append(token.CopyText);
                }
            }

            return builder.ToString();
        }
    }
}