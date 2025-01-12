using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
    
    //UI bindings
    private bool selectedModelEditable = false;
    private string? selectedModel = null;
    private string? selectedExpression = null;
    private bool parameterEditingEnabled = false;
    private double minimumScale = 0.01;
    private double maximumScale = 1.5;
    private double scaleStep = 0.01;
    private double modelScale;
    private L2dModel? selectedModelObject;
    MudTabs? tabs;
    

    protected override async Task OnInitializedAsync()
    {
        Live2DMapping = await HttpClient.GetFromJsonAsync<Dictionary<string, string>>("L2D/mapping.json", new JsonSerializerOptions()
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        });
        Live2DMapping = Live2DMapping!.OrderBy(x => x.Key).ToDictionary();
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
        if (module is not null && Live2DMapping is not null && selectedModel is not null)
        {
            try
            {
                selectedModelObject = await module.InvokeAsync<L2dModel>("loadModel",
                    $"L2D/{Live2DMapping[selectedModel]}/{Live2DMapping[selectedModel]}.model3.json",
                    selectedModelEditable);
                if (selectedModelObject is not null)
                {
                    ResetValues();

                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Failed to load the selected model. Please check the console for more details.", Severity.Error);
                Console.WriteLine(ex.Message);
                selectedModelObject = null;
                modelScale = 0;
            }

            await InvokeAsync(() => StateHasChanged());
        }
    }

    public void ResetValues()
    {
        modelScale = selectedModelObject?.Scale ?? 0.2;
        selectedExpression = null;
        parameterEditingEnabled = false;
        if (tabs is not null)
        {
            tabs.ActivatePanel(0);
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

    public async Task OnParameterEditingEnabledChange()
    {
        Console.WriteLine($"Parameter editing is now {parameterEditingEnabled}");
        if (module is not null)
        {
            await module.InvokeVoidAsync("enableParameterEditing", parameterEditingEnabled);
            await InvokeAsync(() => StateHasChanged());
        }
    }

    [JSInvokable]
    public async Task OnL2dScroll(WheelEventArgs args)
    {
        if (isScrollUp(args.DeltaY) && modelScale < maximumScale)
        {
            modelScale += scaleStep * 5;
        }
        else if (modelScale > minimumScale)
        {
            modelScale -= scaleStep * 5;
        }
        //Convert double to 2 decimals to remove imprecision on the above operations
        modelScale = Convert.ToDouble(modelScale.ToString("#.00"));
        OnModelScaleChanged();
        await InvokeAsync(() => StateHasChanged());
        
        bool isScrollUp(double delta) => delta < 0;
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
    
    public record L2dParameterValueRanges(Dictionary<int,double> MinValues, Dictionary<int,double> MaxValues, double[] DefaultValues);

    public record L2dModel(Dictionary<string, L2dMotion[]>? Motions, L2dExpression[]? Expressions, double Scale, 
        string[]? Parameters, L2dParameterValueRanges ParametersValueRange);
}