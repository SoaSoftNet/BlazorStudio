﻿using BlazorStudio.ClassLib.Store.DialogCase;
using BlazorStudio.ClassLib.Store.DropdownCase;
using BlazorStudio.ClassLib.Store.MenuCase;
using BlazorStudio.ClassLib.UserInterface;
using BlazorStudio.RazorLib.InputFile;
using Fluxor;
using Microsoft.AspNetCore.Components;

namespace BlazorStudio.RazorLib.Shared;

public partial class ToolbarDisplay : ComponentBase
{
    [Inject]
    private IState<DialogStates> DialogStatesWrap { get; set; } = null!;
    [Inject]
    private IDispatcher Dispatcher { get; set; } = null!;

    private readonly DialogRecord _inputFileDialog = new DialogRecord(
        DialogKey.NewDialogKey(),
        "Input File",
        typeof(InputFileDialog),
        null
    );

    private Dimensions _fileDropdownDimensions = new()
    {
        DimensionsPositionKind = DimensionsPositionKind.Absolute,
        LeftCalc = new List<DimensionUnit>
        {
            new()
            {
                DimensionUnitKind = DimensionUnitKind.RootCharacterHeight,
                Value = -2
            }
        },
        TopCalc = new List<DimensionUnit>
        {
            new()
            {
                DimensionUnitKind = DimensionUnitKind.RootCharacterHeight,
                Value = 0.7
            }
        },
    };

    private DropdownKey _fileDropdownKey = DropdownKey.NewDropdownKey();

    private void DispatchAddActiveDropdownKeyActionOnClick(DropdownKey fileDropdownKey)
    {
        Dispatcher.Dispatch(new AddActiveDropdownKeyAction(fileDropdownKey));
    }

    private void OpenInputFileDialog()
    {
        if (DialogStatesWrap.Value.List.All(x => x.DialogKey != _inputFileDialog.DialogKey))
            Dispatcher.Dispatch(new RegisterDialogAction(_inputFileDialog));
    }

    private IEnumerable<MenuOptionRecord> GetMenuOptionRecords()
    {
        var openFolder = MenuOptionFacts.File
            .ConstructOpenFolder(OpenInputFileDialog);

        return new[] { openFolder };
    }
}