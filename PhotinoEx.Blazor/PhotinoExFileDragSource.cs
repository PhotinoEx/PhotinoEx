using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using PhotinoEx.Core.Models;

namespace PhotinoEx.Blazor;

[UnsupportedOSPlatform("macos")]
public sealed class PhotinoExFileDragSource : ComponentBase
{
    [Inject]
    private IPhotinoExFileDragDrop FileDragDrop { get; set; } = null!;

    [Parameter, EditorRequired]
    public IReadOnlyList<string> Paths { get; set; } = [];

    [Parameter]
    public FileDragDropEffects AllowedEffects { get; set; } = FileDragDropEffects.Copy;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [Parameter]
    public EventCallback<FileDragDropEffects> DragCompleted { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", Class);
        builder.AddAttribute(
            3,
            "onpointerdown",
            EventCallback.Factory.Create<PointerEventArgs>(this, BeginDrag)
        );
        builder.AddContent(4, ChildContent);
        builder.CloseElement();
    }

    private async Task BeginDrag(PointerEventArgs _)
    {
        var result = await FileDragDrop.BeginDragAsync(Paths, AllowedEffects);
        await DragCompleted.InvokeAsync(result);
    }
}
