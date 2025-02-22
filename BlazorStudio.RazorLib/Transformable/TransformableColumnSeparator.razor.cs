﻿using BlazorStudio.ClassLib.Store.DragCase;
using BlazorStudio.ClassLib.UserInterface;
using Fluxor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorStudio.RazorLib.Transformable;

public partial class TransformableColumnSeparator : ComponentBase, IDisposable
{
    [Inject]
    private IState<DragState> DragStateWrap { get; set; } = null!;
    [Inject]
    private IDispatcher Dispatcher { get; set; } = null!;

    [Parameter, EditorRequired]
    public Dimensions LeftDimensions { get; set; } = null!;
    [Parameter, EditorRequired]
    public Dimensions RightDimensions { get; set; } = null!;
    [Parameter, EditorRequired]
    public Func<Task> ReRenderFunc { get; set; } = null!;

    private Func<MouseEventArgs, Task>? _dragStateEventHandler;
    private MouseEventArgs? _previousDragMouseEventArgs;

    protected override void OnInitialized()
    {
        DragStateWrap.StateChanged += DragState_StateChanged;

        base.OnInitialized();
    }

    private async void DragState_StateChanged(object? sender, EventArgs e)
    {
        if (!DragStateWrap.Value.IsDisplayed)
        {
            _dragStateEventHandler = null;
            _previousDragMouseEventArgs = null;
        }
        else
        {
            var mouseEventArgs = DragStateWrap.Value.MouseEventArgs;

            if (_dragStateEventHandler is not null)
            {
                if (_previousDragMouseEventArgs is not null)
                {
                    await _dragStateEventHandler(mouseEventArgs);
                }

                _previousDragMouseEventArgs = mouseEventArgs;

                await ReRenderFunc();
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private void SubscribeToDragEventWithEastResizeHandle()
    {
        _dragStateEventHandler = DragEventHandlerEastResizeHandle;
        Dispatcher.Dispatch(new SetDragStateAction(true, null));
    }

    private async Task DragEventHandlerEastResizeHandle(MouseEventArgs mouseEventArgs)
    {
        var deltaX = mouseEventArgs.ClientX - _previousDragMouseEventArgs!.ClientX;

        var leftWidthPixelOffset = LeftDimensions.WidthCalc.FirstOrDefault(x => x.DimensionUnitKind == DimensionUnitKind.Pixels);

        if (leftWidthPixelOffset is null)
        {
            leftWidthPixelOffset = new()
            {
                DimensionUnitKind = DimensionUnitKind.Pixels,
                DimensionUnitOperationKind = DimensionUnitOperationKind.Add,
                Value = 0
            };

            LeftDimensions.WidthCalc.Add(leftWidthPixelOffset);
        }

        leftWidthPixelOffset.Value += deltaX;

        var rightWidthPixelOffset = RightDimensions.WidthCalc.FirstOrDefault(x => x.DimensionUnitKind == DimensionUnitKind.Pixels);

        if (rightWidthPixelOffset is null)
        {
            rightWidthPixelOffset = new()
            {
                DimensionUnitKind = DimensionUnitKind.Pixels,
                DimensionUnitOperationKind = DimensionUnitOperationKind.Add,
                Value = 0
            };

            RightDimensions.WidthCalc.Add(rightWidthPixelOffset);
        }

        rightWidthPixelOffset.Value -= deltaX;
    }

    public void Dispose()
    {
        DragStateWrap.StateChanged -= DragState_StateChanged;
    }
}