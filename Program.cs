using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Elsa.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ElsaConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = @"C:\Users\Alex\Downloads\Workflows\";
            foreach (var file in Directory.GetFiles(path))
            {
                var previousWF = JsonConvert.DeserializeObject<ElsaConverter.Previous.Elsa.Models.WorkflowDefinitionVersion>(File.ReadAllText(file));

                var startActivityId = previousWF.Activities.Where(c => c.Type == "Start").FirstOrDefault().Id;
                previousWF.DefinitionId = previousWF.Id = Path.GetFileNameWithoutExtension(file).Split(".").Last();
                previousWF.Name = Path.GetFileNameWithoutExtension(file).Split(".").Skip(1).FirstOrDefault();
                Elsa.Models.WorkflowDefinition workflowDefinition = new Elsa.Models.WorkflowDefinition
                {
                    Id = previousWF.Id,
                    DefinitionId = previousWF.DefinitionId,
                    Description = previousWF.Description,
                    IsLatest = previousWF.IsLatest,
                    IsPublished = previousWF.IsPublished,
                    IsSingleton = previousWF.IsSingleton,
                    Name = previousWF.Name,
                    Version = previousWF.Version,
                    Activities = previousWF.Activities.Where(c => c.Type != "Start").Select(c => new ActivityDefinition
                    {
                        Name = c.Name,
                        DisplayName = c.DisplayName,
                        Description = c.Description,
                        ActivityId = c.Id,
                        Properties = buildProperties(c.State),
                        Type = c.Type switch
                        {
                            "CronEvent" => "Cron",
                            "Signaled" => "SignalReceived",
                            _ => c.Type
                        },
                        //c.State.ToObject<ElsaConverter.Previous.State>().Variables.Select(c => new ActivityDefinitionProperty { Expressions = c.Value == null?null: new Dictionary<string, string> { [c.Value.Syntax] = c.Value?.Expression?.ToString() }, Name = c.Key, Syntax = c.Value?.Syntax }).ToList()
                    }).ToList(),
                    Connections = previousWF.Connections.Where(c => c.SourceActivityId != startActivityId).Select(c => new ConnectionDefinition { Outcome = c.Outcome, SourceActivityId = c.SourceActivityId, TargetActivityId = c.DestinationActivityId }).ToList()
                };

                foreach (var item in workflowDefinition.Activities.Where(c => c.Name == null || c.DisplayName == null || c.Description == null))
                {
                    var nameProp = item.Properties.FirstOrDefault(c => c.Name == "name");
                    if (nameProp != null)
                        item.Name = nameProp.Expressions?.FirstOrDefault().Value;
                    else item.Name = item.Type;
                    var displayProp = item.Properties.FirstOrDefault(c => c.Name == "title");
                    if (displayProp != null)
                        item.DisplayName = displayProp.Expressions?.FirstOrDefault().Value;
                    else item.DisplayName = item.Type;
                    var desc = item.Properties.FirstOrDefault(c => c.Name == "description");
                    if (desc != null)
                        item.Description = desc.Expressions?.FirstOrDefault().Value;
                }

                var serializerSettings = new JsonSerializerSettings();
                serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                serializerSettings.Formatting = Formatting.Indented;
                var resultJson = JsonConvert.SerializeObject(workflowDefinition, serializerSettings);
                resultJson = resultJson.Replace("json", "Json").Replace("branches", "Branches").Replace("literal", "Literal").Replace("liquid", "Liquid").Replace("javaScript", "JavaScript");
                File.WriteAllText(Path.Combine(path, "out", Path.GetFileName(file)), resultJson);
            }
        }

        private static ICollection<ActivityDefinitionProperty> buildProperties(JObject state)
        {
            List<ActivityDefinitionProperty> res = new List<ActivityDefinitionProperty>();

            foreach (var item in state)
            {
                if (item.Key == "valueExpression")
                {
                    res.Add(new ActivityDefinitionProperty { Name = "Value", Syntax = item.Value.Value<string>("syntax"), Expressions = new Dictionary<string, string> { [item.Value.Value<string>("syntax")] = item.Value.Value<string>("expression") } });
                }
                else {
                    var newAct =
                    new ActivityDefinitionProperty
                    {
                        Name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(item.Key)/*item.Key switch
                        {
                            "branches" => "Branches",
                            _ => item.Key
                        }*/

                        ,
                        Expressions = item.Value.HasValues ? new Dictionary<string, string>
                        {
                            [
                            item.Value.Contains("syntax") ?
                            item.Value?.Value<string>("syntax") : "Literal"] =
                            item.Value.SelectToken("expression") != null ?
                            item.Value?.Value<string>("expression") : item.Value.ToString()
                        } :
                            new Dictionary<string, string>
                            {
                                ["value"] = item.Value.ToString()
                            }
                    };

                    if (newAct.Name == "Branches")
                        newAct.Expressions["Json"] = newAct.Expressions["Literal"];

                    res.Add(newAct);
                }

            }
            return res;
        }
    }
}
