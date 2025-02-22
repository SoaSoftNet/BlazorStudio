﻿using BlazorStudio.ClassLib.Store.DragCase;
using BlazorStudio.ClassLib.UserInterface;
using Fluxor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorStudio.RazorLib.Transformable;

public partial class TransformableRowSeparator : ComponentBase, IDisposable
{
    [Inject]
    private IState<DragState> DragStateWrap { get; set; } = null!;
    [Inject]
    private IDispatcher Dispatcher { get; set; } = null!;

    [Parameter, EditorRequired]
    public Dimensions TopDimensions { get; set; } = null!;
    [Parameter, EditorRequired]
    public Dimensions BottomDimensions { get; set; } = null!;
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

    private void SubscribeToDragEventWithNorthResizeHandle()
    {
        _dragStateEventHandler = DragEventHandlerNorthResizeHandle;
        Dispatcher.Dispatch(new SetDragStateAction(true, null));
    }

    private async Task DragEventHandlerNorthResizeHandle(MouseEventArgs mouseEventArgs)
    {
        var deltaY = mouseEventArgs.ClientY - _previousDragMouseEventArgs!.ClientY;

        var topHeightPixelOffset = TopDimensions.HeightCalc.FirstOrDefault(x => x.DimensionUnitKind == DimensionUnitKind.Pixels);

        if (topHeightPixelOffset is null)
        {
            topHeightPixelOffset = new()
            {
                DimensionUnitKind = DimensionUnitKind.Pixels,
                DimensionUnitOperationKind = DimensionUnitOperationKind.Add,
                Value = 0
            };

            TopDimensions.HeightCalc.Add(topHeightPixelOffset);
        }

        topHeightPixelOffset.Value += deltaY;
        
        var bottomHeightPixelOffset = BottomDimensions.HeightCalc.FirstOrDefault(x => x.DimensionUnitKind == DimensionUnitKind.Pixels);

        if (bottomHeightPixelOffset is null)
        {
            bottomHeightPixelOffset = new()
            {
                DimensionUnitKind = DimensionUnitKind.Pixels,
                DimensionUnitOperationKind = DimensionUnitOperationKind.Add,
                Value = 0
            };

            BottomDimensions.HeightCalc.Add(bottomHeightPixelOffset);
        }

        bottomHeightPixelOffset.Value -= deltaY;
    }

    public void Dispose()
    {
        DragStateWrap.StateChanged -= DragState_StateChanged;
    }
}