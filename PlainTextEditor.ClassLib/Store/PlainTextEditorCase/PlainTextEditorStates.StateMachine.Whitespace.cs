using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlainTextEditor.ClassLib.Keyboard;

namespace PlainTextEditor.ClassLib.Store.PlainTextEditorCase;

public partial record PlainTextEditorStates
{
    private partial class StateMachine
    {
        public static PlainTextEditorRecord HandleWhitespace(PlainTextEditorRecord focusedPlainTextEditorRecord,
            KeyDownEventRecord keyDownEventRecord)
        {
            var rememberToken = focusedPlainTextEditorRecord
                    .GetCurrentTextTokenAs<TextTokenBase>();

            if (rememberToken.IndexInPlainText!.Value != rememberToken.PlainText.Length - 1)
            {
                if (KeyboardKeyFacts.NewLineCodes.ALL_NEW_LINE_CODES.Contains(keyDownEventRecord.Code))
                {
                    focusedPlainTextEditorRecord = SplitCurrentToken(
                        focusedPlainTextEditorRecord,
                        null
                    );

                    return InsertNewLine(focusedPlainTextEditorRecord,
                        keyDownEventRecord);
                }

                return SplitCurrentToken(
                    focusedPlainTextEditorRecord,
                        new WhitespaceTextToken(keyDownEventRecord)
                );
            }
            else
            {
                if (KeyboardKeyFacts.NewLineCodes.ALL_NEW_LINE_CODES.Contains(keyDownEventRecord.Code))
                {
                    return InsertNewLine(focusedPlainTextEditorRecord,
                        keyDownEventRecord);
                }

                return InsertNewCurrentTokenAfterCurrentPosition(focusedPlainTextEditorRecord,
                    new WhitespaceTextToken(keyDownEventRecord));
            }
        }
    }
}
