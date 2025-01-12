using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace GirlsFrontline2Live2DViewer.Client.Pages;

public partial class Index
{
    [Inject] public IJSRuntime JsRuntime { get; set; }

    [Inject] public HttpClient HttpClient { get; set; }
    
    [Inject] public ISnackbar Snackbar { get; set; }

    private IJSObjectReference? module;
    private Dictionary<string, string>? Live2DMapping = null;
    public bool selectedModelEditable = false;
    public string? selectedModel = null;
    public string? selectedExpression = null;
    public bool eyeTrackingEnabled = false;
    public double modelScale;
    public L2dModel? selectedModelObject;

    protected override async Task OnInitializedAsync()
    {
        Live2DMapping = await HttpClient.GetFromJsonAsync<Dictionary<string, string>>("L2D/mapping.json", new JsonSerializerOptions()
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        });
        Live2DMapping = Live2DMapping.OrderBy(x => x.Key).ToDictionary();
    }

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./Pages/Index.razor.js");
        }
    }

    public async void OnSelectedModelChanged()
    {
        Console.WriteLine($"Selected model is now at {selectedModel}");
        if (module is not null)
        {
            try
            {
                selectedModelObject = await module.InvokeAsync<L2dModel>("loadModel",
                    $"L2D/{Live2DMapping[selectedModel]}/{Live2DMapping[selectedModel]}.model3.json",
                    selectedModelEditable);
                if (selectedModelObject is not null)
                {
                    Console.WriteLine("Selected model from js is not null");
                    Console.WriteLine(selectedModelObject.Motions?.Count);
                    Console.WriteLine(selectedModelObject.Expressions?.Length);
                    modelScale = selectedModelObject.Scale;
                    selectedExpression = null;

                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Failed to load the selected model", Severity.Error);
                selectedModelObject = null;
                modelScale = 0;
            }

            await InvokeAsync(() => StateHasChanged());
        }
    }

    public async void OnEyeTrackingEnabled()
    {
        Console.WriteLine($"Eye tracking set to {eyeTrackingEnabled}");

        if (module is not null)
        {
            await module.InvokeVoidAsync("setEyeTracking", eyeTrackingEnabled);
            await InvokeAsync(() => StateHasChanged());
        }
    }

    public async void OnSelectedExpressionChanged()
    {
        Console.WriteLine($"Selected expression is now {selectedExpression}");
        if (module is not null && selectedExpression is not null)
        {
            await module.InvokeVoidAsync("setExpression", selectedExpression);
            await InvokeAsync(() => StateHasChanged());
        }
    }

    public async Task OnMotionButtonClicked(string motion)
    {
        Console.WriteLine($"Selected motion {motion}");
        if (module is not null)
        {
            await module.InvokeVoidAsync("setMotion", motion);
            await InvokeAsync(() => StateHasChanged());
        }
    }
    
    private async void OnModelScaleChanged()
    {
        Console.WriteLine($"Changing scale to {modelScale}");
        if (module is not null)
        {
            await module.InvokeVoidAsync("setScale", modelScale);
            await InvokeAsync(() => StateHasChanged());
        }
    }

    public async Task OnModelParameterChanged(string parameter, double value)
    {
        Console.WriteLine($"Changing parameter {parameter} to {value}");
        if (module is not null)
        {
            await module.InvokeVoidAsync("setParameter", parameter, value);
            await InvokeAsync(() => StateHasChanged());
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (module is not null)
        {
            try
            {
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }

    public record L2dExpression(string Name, string FilePath);

    public record L2dMotion(string File);

    public record L2dModel(Dictionary<string, L2dMotion[]>? Motions, L2dExpression[]? Expressions, double Scale, 
        string[]? Parameters, bool Editable = false);
}