﻿using BlazorStudio.ClassLib.Store.DialogCase;
using BlazorStudio.RazorLib.Settings;
using Fluxor;
using Microsoft.AspNetCore.Components;

namespace BlazorStudio.RazorLib.TaskModelManager;

public partial class TaskModelManagerDialogEntryPointDisplay : ComponentBase
{
    [Inject]
    private IState<DialogStates> DialogStatesWrap { get; set; } = null!;
    [Inject]
    private IDispatcher Dispatcher { get; set; } = null!;

    private readonly DialogRecord _taskModelManagerDialog = new DialogRecord(
        DialogKey.NewDialogKey(),
        "Task Manager",
        typeof(TaskModelManagerDialogDisplay),
        null
    );

    private void OpenTaskModelManagerDialogOnClick()
    {
        if (DialogStatesWrap.Value.List.All(x => x.DialogKey != _taskModelManagerDialog.DialogKey))
            Dispatcher.Dispatch(new RegisterDialogAction(_taskModelManagerDialog));
    }
}