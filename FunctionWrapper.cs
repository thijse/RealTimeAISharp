using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using OpenAI.RealtimeConversation;
using NJsonSchema;
using RealtimeInteractiveConsole.Utilities.RealtimeInteractiveConsole.Utilities;

namespace RealtimeInteractiveConsole;

public class ConversationFunctionWrapper
{
    private readonly MethodInfo _method;
    private readonly object _target;

    public ConversationFunctionWrapper(object target, MethodInfo method)
    {
        _target = target;
        _method = method;
    }

    public ConversationFunctionWrapper(Delegate function) : this(function.Target, function.Method)
    {
    }

    public ConversationFunctionTool CreateTool()
    {
        var tool = new ConversationFunctionTool
        {
            Name = _method.Name,
            Description = GetMethodDescription(),
            Parameters = BinaryData.FromString(GenerateParametersJson())
        };

        return tool;
    }

    //public async Task<T> InvokeFunctionAsync<T>(ConversationItemFinishedUpdate itemFinishedUpdate)
    //{
    //    if (itemFinishedUpdate.FunctionName != _method.Name) return null;

    //    // Deserialize the JSON object into a dictionary
    //    var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(itemFinishedUpdate.FunctionCallArguments);
    //    //if (parameters != null || parameters.Count > 0) return null;

    //    // Use the method and the provided parameters to invoke the function, casting as necessary
    //    var parameterValues = _method.GetParameters()
    //        .Select(p => p.Name != null && parameters != null && parameters.TryGetValue(p.Name, out var parameter) ? DeserializeParameter(parameter, p.ParameterType) : Type.Missing)
    //        .ToArray();

    //    object? result = null;
    //    try
    //    {
    //        result = _method.Invoke(_target, parameterValues);
    //    }
    //    catch (Exception e)
    //    {
    //        Debug.WriteLine($"Error invoking function {_method.Name}: {e.Message}");
    //        return null;
    //    }

    //    if (result is Task task)
    //    {
    //        await task;
    //        result = ((dynamic)task).Result;
    //    }

    //    return result;
    //}

    public async Task<T?> InvokeFunctionAsync<T>(ConversationItemFinishedUpdate itemFinishedUpdate)
    {
        if (itemFinishedUpdate.FunctionName != _method.Name) return default;

        // Deserialize the JSON object into a dictionary
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(itemFinishedUpdate.FunctionCallArguments);
        if (parameters == null || parameters.Count == 0) return default;

        // Use the method and the provided parameters to invoke the function, casting as necessary
        var parameterValues = _method.GetParameters()
            .Select(p => parameters.ContainsKey(p.Name) ? DeserializeParameter(parameters[p.Name], p.ParameterType) : Type.Missing)
            .ToArray();

        object? result;
        try
        {
            result = _method.Invoke(_target, parameterValues);
        }
        catch (Exception e)
        {
            Output.WriteLine($"Error invoking function {_method.Name}: {e.Message}");
            return default;
        }

        if (result is Task task)
        {
            await task;
            result = ((dynamic)task).Result;
        }

        return (T?)result;
    }


    public async Task<ConversationItem?> InvokeFunctionForConversationAsync(ConversationItemFinishedUpdate itemFinishedUpdate)
    {
        var result = await InvokeFunctionAsync<object?>(itemFinishedUpdate);
        if (result==null) return null;
        return ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, result.ToString());
    }

    private object DeserializeParameter(object parameter, Type targetType)
    {
        if (parameter is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize(jsonElement.GetRawText(), targetType) ?? "";
        }
        return parameter;
    }


    private string GetMethodDescription()
    {
        // Get the description from the Method's custom attributes if it exists
        var descriptionAttr = _method.GetCustomAttribute<DescriptionAttribute>();
        return descriptionAttr != null ? descriptionAttr.Description : "No description available.";
    }

    private string GenerateParametersJson()
    {
        // Generates the JSON schema for the method parameters
        var parameters = _method.GetParameters();
        if (parameters.Length == 0) return "{}";
        var properties = parameters.Select(p => $"\"{p.Name}\": {{\n                \"type\": \"{GetJsonType(p.ParameterType)}\",\n                \"description\": \"{GetParameterDescription(p)}\"\n            }}").ToArray();

        return $"{{ \"type\": \"object\", \"properties\": {{ {string.Join(",", properties)} }}, \"required\": [{string.Join(",", parameters.Select(p => $"\"{p.Name}\""))}] }}";
    }

    private string GetJsonType(Type type)
    {
        // Use NJsonSchema to get the JSON type of the parameter
        var schema = JsonSchema.FromType(type);
        return schema.Type.ToString().ToLower();
    }

    private string GetParameterDescription(ParameterInfo parameter)
    {
        // Gets the description for the parameter if it has a DescriptionAttribute
        var descriptionAttr = parameter.GetCustomAttribute<DescriptionAttribute>();
        return descriptionAttr != null ? descriptionAttr.Description : "No description available.";
    }
}

