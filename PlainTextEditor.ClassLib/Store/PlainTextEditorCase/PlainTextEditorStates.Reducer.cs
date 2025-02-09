using Fluxor;
using PlainTextEditor.ClassLib.Sequence;
using PlainTextEditor.ClassLib.Store.KeyDownEventCase;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using BlazorStudio.Shared.FileSystem.Classes;
using PlainTextEditor.ClassLib.Keyboard;

namespace PlainTextEditor.ClassLib.Store.PlainTextEditorCase;

public partial record PlainTextEditorStates
{
    public class PlainTextEditorStatesReducer
    {
        private readonly IState<PlainTextEditorStates> _plainTextEditorStatesWrap;

        public PlainTextEditorStatesReducer(IState<PlainTextEditorStates> plainTextEditorStatesWrap)
        {
            _plainTextEditorStatesWrap = plainTextEditorStatesWrap;
        }

        private readonly SemaphoreSlim _effectSemaphoreSlim = new(1, 1);
        private readonly ConcurrentQueue<Func<Task>> _concurrentQueueForEffects = new();

        private int _queuedEffectsCounter;

        [ReducerMethod]
        public static PlainTextEditorStates ReduceConstructPlainTextEditorAction(PlainTextEditorStates previousPlainTextEditorStates,
            ConstructPlainTextEditorRecordAction constructPlainTextEditorRecordAction)
        {
            var nextPlainTextEditorMap = new Dictionary<PlainTextEditorKey, IPlainTextEditor>(previousPlainTextEditorStates.Map);
            var nextPlainTextEditorList = new List<PlainTextEditorKey>(previousPlainTextEditorStates.Array);

            var plainTextEditor = new
                PlainTextEditorRecord(constructPlainTextEditorRecordAction.PlainTextEditorKey);

            nextPlainTextEditorMap[constructPlainTextEditorRecordAction.PlainTextEditorKey] = plainTextEditor;
            nextPlainTextEditorList.Add(constructPlainTextEditorRecordAction.PlainTextEditorKey);

            return new PlainTextEditorStates(nextPlainTextEditorMap.ToImmutableDictionary(), nextPlainTextEditorList.ToImmutableArray());
        }

        [ReducerMethod]
        public static PlainTextEditorStates ReduceDeconstructPlainTextEditorRecordAction(PlainTextEditorStates previousPlainTextEditorStates,
            DeconstructPlainTextEditorRecordAction deconstructPlainTextEditorRecordAction)
        {
            var nextPlainTextEditorMap = new Dictionary<PlainTextEditorKey, IPlainTextEditor>(previousPlainTextEditorStates.Map);
            var nextPlainTextEditorList = new List<PlainTextEditorKey>(previousPlainTextEditorStates.Array);

            nextPlainTextEditorMap.Remove(deconstructPlainTextEditorRecordAction.PlainTextEditorKey);
            nextPlainTextEditorList.Remove(deconstructPlainTextEditorRecordAction.PlainTextEditorKey);

            return new PlainTextEditorStates(nextPlainTextEditorMap.ToImmutableDictionary(), nextPlainTextEditorList.ToImmutableArray());
        }

        [EffectMethod]
        public async Task HandleKeyDownEventAction(KeyDownEventAction keyDownEventAction,
            IDispatcher dispatcher)
        {
            _queuedEffectsCounter++;

            try
            {
                _concurrentQueueForEffects.Enqueue(async () =>
                {
                    var previousPlainTextEditorStates = _plainTextEditorStatesWrap.Value;

                    var nextPlainTextEditorMap = new Dictionary<PlainTextEditorKey, IPlainTextEditor>(previousPlainTextEditorStates.Map);
                    var nextPlainTextEditorList = new List<PlainTextEditorKey>(previousPlainTextEditorStates.Array);

                    var focusedPlainTextEditor = previousPlainTextEditorStates.Map[keyDownEventAction.FocusedPlainTextEditorKey]
                        as PlainTextEditorRecord;

                    if (focusedPlainTextEditor is null)
                        return;

                    // TODO: This is not a good way to do Keymaps but am testing save functionality before building Keymaps logic
                    //if (keyDownEventAction.KeyDownEventRecord.CtrlWasPressed)
                    //{
                    //    if (keyDownEventAction.KeyDownEventRecord.Key == "s" ||
                    //        keyDownEventAction.KeyDownEventRecord.Key == "S")
                    //    {
                    //        await SavePlainTextEditor(focusedPlainTextEditor);
                    //        return;
                    //    }
                    //}

                    var overrideKeyDownEventRecord = keyDownEventAction.KeyDownEventRecord;

                    if (keyDownEventAction.KeyDownEventRecord.Code == KeyboardKeyFacts.NewLineCodes.ENTER_CODE &&
                        focusedPlainTextEditor.UseCarriageReturnNewLine)
                    {
                        overrideKeyDownEventRecord = keyDownEventAction.KeyDownEventRecord with
                        {
                            Code = KeyboardKeyFacts.NewLineCodes.CARRIAGE_RETURN_NEW_LINE_CODE
                        };
                    }

                    var replacementPlainTextEditor = PlainTextEditorStates.StateMachine
                        .HandleKeyDownEvent(focusedPlainTextEditor, overrideKeyDownEventRecord) with
                    {
                        SequenceKey = SequenceKey.NewSequenceKey()
                    };

                    nextPlainTextEditorMap[keyDownEventAction.FocusedPlainTextEditorKey] = replacementPlainTextEditor;

                    var nextImmutableMap = nextPlainTextEditorMap.ToImmutableDictionary();
                    var nextImmutableArray = nextPlainTextEditorList.ToImmutableArray();

                    _queuedEffectsCounter--;

                    dispatcher.Dispatch(
                        new SetPlainTextEditorStatesAction(
                            new PlainTextEditorStates(nextImmutableMap, nextImmutableArray)));
                });

                await _effectSemaphoreSlim.WaitAsync();

                if (_concurrentQueueForEffects.TryDequeue(out var effect))
                    await effect.Invoke();
            }
            finally
            {
                _effectSemaphoreSlim.Release();
            }
        }

        [EffectMethod]
        public async Task HandleInitializeAction(PlainTextEditorInitializeAction plainTextEditorInitializeAction,
            IDispatcher dispatcher)
        {
            _queuedEffectsCounter++;

            try
            {
                _concurrentQueueForEffects.Enqueue(async () =>
                {
                    var previousPlainTextEditorStates = _plainTextEditorStatesWrap.Value;

                    var nextPlainTextEditorMap = new Dictionary<PlainTextEditorKey, IPlainTextEditor>(previousPlainTextEditorStates.Map);
                    var nextPlainTextEditorList = new List<PlainTextEditorKey>(previousPlainTextEditorStates.Array);

                    var fileCoordinateGrid = await FileCoordinateGridFactory
                        .ConstructFileCoordinateGridAsync(plainTextEditorInitializeAction.AbsoluteFilePath);

                    var plainTextEditor = new
                        PlainTextEditorRecord(plainTextEditorInitializeAction.PlainTextEditorKey)
                    {
                        FileCoordinateGrid = fileCoordinateGrid
                    };

                    PlainTextEditorRecord replacementPlainTextEditor = plainTextEditor;

                    var allEnterKeysAreCarriageReturnNewLine = true;
                    var seenEnterKey = false;
                    var previousCharacterWasCarriageReturn = false;

                    string MutateIfPreviousCharacterWasCarriageReturn()
                    {
                        seenEnterKey = true;

                        if (!previousCharacterWasCarriageReturn)
                        {
                            allEnterKeysAreCarriageReturnNewLine = false;
                        }

                        return previousCharacterWasCarriageReturn
                            ? KeyboardKeyFacts.WhitespaceKeys.CARRIAGE_RETURN_NEW_LINE_CODE
                            : KeyboardKeyFacts.WhitespaceKeys.ENTER_CODE;
                    }

                    var content = await plainTextEditor.FileCoordinateGrid
                        .Request(new FileCoordinateGridRequest(0, 1000, CancellationToken.None));

                    foreach (var character in content)
                    {
                        if (character == '\r')
                        {
                            previousCharacterWasCarriageReturn = true;
                            continue;
                        }

                        var code = character switch
                        {
                            '\t' => KeyboardKeyFacts.WhitespaceKeys.TAB_CODE,
                            ' ' => KeyboardKeyFacts.WhitespaceKeys.SPACE_CODE,
                            '\n' => MutateIfPreviousCharacterWasCarriageReturn(),
                            _ => character.ToString()
                        };

                        var keyDown = new KeyDownEventAction(plainTextEditorInitializeAction.PlainTextEditorKey,
                            new KeyDownEventRecord(
                                character.ToString(),
                                code,
                                false,
                                false,
                                false
                            )
                        );

                        replacementPlainTextEditor = PlainTextEditorStates.StateMachine
                                .HandleKeyDownEvent(replacementPlainTextEditor, keyDown.KeyDownEventRecord) with
                        {
                            SequenceKey = SequenceKey.NewSequenceKey()
                        };

                        previousCharacterWasCarriageReturn = false;
                    }

                    if (seenEnterKey && allEnterKeysAreCarriageReturnNewLine)
                    {
                        replacementPlainTextEditor = replacementPlainTextEditor with
                        {
                            UseCarriageReturnNewLine = true
                        };
                    }

                    nextPlainTextEditorMap[plainTextEditorInitializeAction.PlainTextEditorKey] = replacementPlainTextEditor;

                    var nextImmutableMap = nextPlainTextEditorMap.ToImmutableDictionary();
                    var nextImmutableArray = nextPlainTextEditorList.ToImmutableArray();

                    await Task.Delay(1);

                    _queuedEffectsCounter--;

                    dispatcher.Dispatch(
                        new SetPlainTextEditorStatesAction(
                            new PlainTextEditorStates(nextImmutableMap, nextImmutableArray)));
                });

                await _effectSemaphoreSlim.WaitAsync();

                if (_concurrentQueueForEffects.TryDequeue(out var effect))
                    await effect.Invoke();
            }
            finally
            {
                _effectSemaphoreSlim.Release();
            }
        }
        
        [EffectMethod]
        public async Task HandleRequestAction(PlainTextEditorRequestAction plainTextEditorRequestAction,
            IDispatcher dispatcher)
        {
            _queuedEffectsCounter++;

            try
            {
                _concurrentQueueForEffects.Enqueue(async () =>
                {
                    var previousPlainTextEditorStates = _plainTextEditorStatesWrap.Value;

                    var nextPlainTextEditorMap = new Dictionary<PlainTextEditorKey, IPlainTextEditor>(previousPlainTextEditorStates.Map);
                    var nextPlainTextEditorList = new List<PlainTextEditorKey>(previousPlainTextEditorStates.Array);

                    var focusedPlainTextEditor = previousPlainTextEditorStates.Map[plainTextEditorRequestAction.FocusedPlainTextEditorKey]
                        as PlainTextEditorRecord;

                    if (focusedPlainTextEditor is null)
                        return;

                    PlainTextEditorRecord replacementPlainTextEditor = focusedPlainTextEditor;

                    var allEnterKeysAreCarriageReturnNewLine = true;
                    var seenEnterKey = false;
                    var previousCharacterWasCarriageReturn = false;

                    string MutateIfPreviousCharacterWasCarriageReturn()
                    {
                        seenEnterKey = true;

                        if (!previousCharacterWasCarriageReturn)
                        {
                            allEnterKeysAreCarriageReturnNewLine = false;
                        }

                        return previousCharacterWasCarriageReturn
                            ? KeyboardKeyFacts.WhitespaceKeys.CARRIAGE_RETURN_NEW_LINE_CODE
                            : KeyboardKeyFacts.WhitespaceKeys.ENTER_CODE;
                    }

                    var content = await focusedPlainTextEditor.FileCoordinateGrid
                        .Request(plainTextEditorRequestAction
                            .FileCoordinateGridRequest);

                    foreach (var character in content)
                    {
                        if (character == '\r')
                        {
                            previousCharacterWasCarriageReturn = true;
                            continue;
                        }

                        var code = character switch
                        {
                            '\t' => KeyboardKeyFacts.WhitespaceKeys.TAB_CODE,
                            ' ' => KeyboardKeyFacts.WhitespaceKeys.SPACE_CODE,
                            '\n' => MutateIfPreviousCharacterWasCarriageReturn(),
                            _ => character.ToString()
                        };

                        var keyDown = new KeyDownEventAction(plainTextEditorRequestAction.FocusedPlainTextEditorKey,
                            new KeyDownEventRecord(
                                character.ToString(),
                                code,
                                false,
                                false,
                                false
                            )
                        );

                        replacementPlainTextEditor = PlainTextEditorStates.StateMachine
                                .HandleKeyDownEvent(replacementPlainTextEditor, keyDown.KeyDownEventRecord) with
                        {
                            SequenceKey = SequenceKey.NewSequenceKey()
                        };

                        previousCharacterWasCarriageReturn = false;
                    }

                    if (seenEnterKey && allEnterKeysAreCarriageReturnNewLine)
                    {
                        replacementPlainTextEditor = replacementPlainTextEditor with
                        {
                            UseCarriageReturnNewLine = true
                        };
                    }

                    nextPlainTextEditorMap[plainTextEditorRequestAction.FocusedPlainTextEditorKey] = replacementPlainTextEditor;

                    var nextImmutableMap = nextPlainTextEditorMap.ToImmutableDictionary();
                    var nextImmutableArray = nextPlainTextEditorList.ToImmutableArray();

                    await Task.Delay(1);

                    _queuedEffectsCounter--;

                    dispatcher.Dispatch(
                        new SetPlainTextEditorStatesAction(
                            new PlainTextEditorStates(nextImmutableMap, nextImmutableArray)));
                });

                await _effectSemaphoreSlim.WaitAsync();

                if (_concurrentQueueForEffects.TryDequeue(out var effect))
                    await effect.Invoke();
            }
            finally
            {
                _effectSemaphoreSlim.Release();
            }
        }

        [ReducerMethod]
        public static PlainTextEditorStates ReduceSetPlainTextEditorStatesAction(PlainTextEditorStates previousPlainTextEditorStates,
            SetPlainTextEditorStatesAction setPlainTextEditorStatesAction)
        {
            return setPlainTextEditorStatesAction.PlainTextEditorStates;
        }

        [ReducerMethod]
        public static PlainTextEditorStates ReducePlainTextEditorOnClickAction(PlainTextEditorStates previousPlainTextEditorStates,
            PlainTextEditorOnClickAction plainTextEditorOnClickAction)
        {
            var nextPlainTextEditorMap = new Dictionary<PlainTextEditorKey, IPlainTextEditor>(previousPlainTextEditorStates.Map);
            var nextPlainTextEditorList = new List<PlainTextEditorKey>(previousPlainTextEditorStates.Array);

            var focusedPlainTextEditor = previousPlainTextEditorStates.Map[plainTextEditorOnClickAction.FocusedPlainTextEditorKey]
                as PlainTextEditorRecord;

            if (focusedPlainTextEditor is null)
                return previousPlainTextEditorStates;

            var replacementPlainTextEditor = PlainTextEditorStates.StateMachine
                .HandleOnClickEvent(focusedPlainTextEditor, plainTextEditorOnClickAction) with
            {
                SequenceKey = SequenceKey.NewSequenceKey()
            };

            nextPlainTextEditorMap[plainTextEditorOnClickAction.FocusedPlainTextEditorKey] = replacementPlainTextEditor;

            return new PlainTextEditorStates(nextPlainTextEditorMap.ToImmutableDictionary(), nextPlainTextEditorList.ToImmutableArray());
        }

        // TODO: Look into Unit Testing save plain text editor before ever allowing a save keybind in the editor (as an aside this ended up working quite well. A current issue is '\r\n' converted to '\n' when saving)
        //private static async Task SavePlainTextEditor(PlainTextEditorRecord focusedPlainTextEditor)
        //{
        //    var documentPlainText = focusedPlainTextEditor.GetPlainText();

        //    await File.WriteAllTextAsync("C:\\Users\\hunte\\source\\repos\\TestBlazorStudio\\main.c",
        //        documentPlainText);
        //}
    }

    /// <summary>
    /// https://stackoverflow.com/questions/4228864/does-lock-guarantee-acquired-in-order-requested
    /// </summary>
    public class QueuedActions
    {
        private readonly object _internalSyncronizer = new object();
        private readonly ConcurrentQueue<Action> _actionsQueue = new ConcurrentQueue<Action>();

        public void Execute(Action action)
        {
            // ReSharper disable once InconsistentlySynchronizedField
            _actionsQueue.Enqueue(action);

            lock (_internalSyncronizer)
            {
                Action nextAction;
                if (_actionsQueue.TryDequeue(out nextAction))
                {
                    nextAction.Invoke();
                }
                else
                {
                    throw new Exception("Something is wrong. How come there is nothing in the queue?");
                }
            }
        }
    }
}

