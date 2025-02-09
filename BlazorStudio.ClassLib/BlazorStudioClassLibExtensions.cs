﻿using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using PlainTextEditor.ClassLib;

namespace BlazorStudio.ClassLib;

public static class BlazorStudioClassLibExtensions
{
    public static IServiceCollection AddBlazorStudioClassLibServices(this IServiceCollection services)
    {
        return services
            .AddFluxor(options => options
                .ScanAssemblies(
                    typeof(BlazorStudioClassLibExtensions).Assembly, 
                    typeof(PlainTextEditorClassLibExtensionMethods).Assembly
                ))
            .AddPlainTextEditorService();
    }
}