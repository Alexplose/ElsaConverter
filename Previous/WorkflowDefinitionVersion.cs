using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElsaConverter.Previous
{
    namespace Elsa.Models
    {
        public class WorkflowDefinitionVersion
        {
            public WorkflowDefinitionVersion()
            {
                Activities = new List<ActivityDefinition>();
                Connections = new List<ConnectionDefinition>();
                Variables = new Variables();
            }

            public WorkflowDefinitionVersion(
                string id,
                string definitionId,
                int version,
                string name,
                string description,
                IEnumerable<ActivityDefinition> activities,
                IEnumerable<ConnectionDefinition> connections,
                bool isSingleton,
                bool isDisabled,
                Variables variables)
            {
                Id = id;
                DefinitionId = definitionId;
                Version = version;
                Name = name;
                Description = description;
                Activities = activities.ToList();
                Connections = connections.ToList();
                IsSingleton = isSingleton;
                IsDisabled = isDisabled;
                Variables = variables;
            }

            public string Id { get; set; }
            public string DefinitionId { get; set; }
            public int Version { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public ICollection<ActivityDefinition> Activities { get; set; }
            public ICollection<ConnectionDefinition> Connections { get; set; }
            public Variables Variables { get; set; }
            public bool IsSingleton { get; set; }
            public bool IsDisabled { get; set; }
            public bool IsPublished { get; set; }
            public bool IsLatest { get; set; }
        }


    }

    public class ActivityDefinition
    {
        public static ActivityDefinition FromActivity(IActivity activity)
        {
            return new ActivityDefinition(activity.Id, activity.Type, activity.State, 0, 0);
        }

        public ActivityDefinition()
        {
            State = new JObject();
        }

        public ActivityDefinition(string id, string type, JObject state, int left = 0, int top = 0)
        {
            Id = id;
            Type = type;
            Left = left;
            Top = top;
            State = new JObject(state);
        }

        public string Id { get; set; }
        public string Type { get; set; }

        public string Name
        {
            get => State.ContainsKey(nameof(Name).ToLower()) ? State[nameof(Name).ToLower()].Value<string>() : default;
            set => State[nameof(Name).ToLower()] = value;
        }


        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public JObject State { get; set; }
    }

    public class ActivityDefinition<T> : ActivityDefinition where T : IActivity
    {
        public ActivityDefinition(string id, JObject state, int left = 0, int top = 0) : base(
            id,
            typeof(T).Name,
            state,
            left,
            top)
        {
        }

        public ActivityDefinition(string id, object state, int left = 0, int top = 0) : base(
            id,
            typeof(T).Name,
            JObject.FromObject(state),
            left,
            top)
        {
        }
    }

    public class ConnectionDefinition
    {
        public ConnectionDefinition()
        {
        }

        public ConnectionDefinition(string sourceActivityId, string destinationActivityId, string outcome)
        {
            SourceActivityId = sourceActivityId;
            DestinationActivityId = destinationActivityId;
            Outcome = outcome;
        }

        public string SourceActivityId { get; set; }
        public string DestinationActivityId { get; set; }
        public string Outcome { get; set; }
    }

    public class State
    {
        public List<string> Names { get; set; }
        public Variables Variables { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class Variables : Dictionary<string, Variable>
    {
        public static readonly Variables Empty = new Variables();


        public Variables()
        {
        }

        public Variables(Variables other) : this((IEnumerable<KeyValuePair<string, Variable>>)other)
        {
        }

        public Variables(IEnumerable<KeyValuePair<string, Variable>> dictionary)
        {
            foreach (var item in dictionary)
                this[item.Key] = item.Value;
        }

        public Variables(IEnumerable<KeyValuePair<string, object>> dictionary)
        {
            foreach (var item in dictionary)
                SetVariable(item.Key, item.Value);
        }

        public object GetVariable(string name)
        {
            return ContainsKey(name) ? this[name].Value : default;
        }

        public T GetVariable<T>(string name)
        {
            object value = GetVariable(name);
            return (value != default)
                ? (T)System.Convert.ChangeType(value, typeof(T))
                : default(T);
        }

        public Variable SetVariable(string name, object value)
        {
            return this[name] = new Variable(value);
        }

        public Variable SetVariable(string name, Variable variable)
        {
            return SetVariable(name, variable.Value);
        }

        public void SetVariables(Variables variables) =>
            SetVariables((IEnumerable<KeyValuePair<string, Variable>>)variables);

        public void SetVariables(IEnumerable<KeyValuePair<string, Variable>> variables)
        {
            foreach (var variable in variables)
                SetVariable(variable.Key, variable.Value);
        }

        public bool HasVariable(string name)
        {
            return ContainsKey(name);
        }
    }

    public class Variable
    {
        public Variable()
        {
        }

        public Variable(object value)
        {
            Value = value;
        }

        [JsonConverter(typeof(TypeNameHandlingConverter))]
        public object Value { get; set; }

        public string Syntax { get; set; }

        public string Expression { get; set; }
    }

    public interface IActivity
    {
        /// <summary>
        /// Holds persistable activity state.
        /// </summary>
        JObject State { get; set; }

        /// <summary>
        /// Holds activity output.
        /// </summary>
        Variables Output { get; set; }

        [JsonIgnore]
        Variables TransientOutput { get; }

        /// <summary>
        /// The type name of this activity.
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Unique identifier of this activity.
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Name identifier of this activity.
        /// </summary>
        string Name { get; set; }

        
    }

    public interface IValueHandler
    {
        int Priority { get; }
        bool CanSerialize(JToken value, Type type);
        bool CanDeserialize(JToken value, Type type);
        object Deserialize(JsonReader reader, JsonSerializer serializer, Type type, JToken value);
        void Serialize(JsonWriter writer, JsonSerializer serializer, Type type, JToken value);
    }

    public class DefaultValueHandler : IValueHandler
    {
        public int Priority => -9000;
        public bool CanSerialize(JToken value, Type type) => true;
        public bool CanDeserialize(JToken value, Type type) => true;
        public object Deserialize(JsonReader reader, JsonSerializer serializer, Type type, JToken value) => serializer.Deserialize(value.CreateReader(), type);
        public void Serialize(JsonWriter writer, JsonSerializer serializer, Type type, JToken value) => serializer.Serialize(writer, value);
    }

    public class TypeNameHandlingConverter : Newtonsoft.Json.JsonConverter
    {
        private static readonly IDictionary<Type, IValueHandler> ValueHandlers = new Dictionary<Type, IValueHandler>();

        public static void RegisterTypeHandler<T>() where T : IValueHandler
        {
            var handler = Activator.CreateInstance<T>();
            RegisterTypeHandler(handler);
        }

        public static void RegisterTypeHandler(IValueHandler handler)
        {
            ValueHandlers[handler.GetType()] = handler;
        }

        static TypeNameHandlingConverter()
        {
            //RegisterTypeHandler<ObjectHandler>();
            //RegisterTypeHandler<DateTimeHandler>();
            //RegisterTypeHandler<InstantHandler>();
            //RegisterTypeHandler<AnnualDateHandler>();
            //RegisterTypeHandler<DurationHandler>();
            //RegisterTypeHandler<LocalDateHandler>();
            //RegisterTypeHandler<LocalDateTimeHandler>();
            //RegisterTypeHandler<LocalTimeHandler>();
            //RegisterTypeHandler<OffsetDateHandler>();
            //RegisterTypeHandler<OffsetHandler>();
            //RegisterTypeHandler<OffsetTimeHandler>();
            //RegisterTypeHandler<YearMonthHandler>();
            //RegisterTypeHandler<ZonedDateTimeHandler>();
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var valueType = value.GetType();
            var token = JToken.FromObject(value);
            var handler = GetHandler(x => x.CanSerialize(token, valueType));

            handler.Serialize(writer, serializer, valueType, token);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.ReadFrom(reader);
            var handler = GetHandler(x => x.CanDeserialize(token, objectType));

            return handler.Deserialize(reader, serializer, objectType, token);
        }

        public override bool CanConvert(Type objectType) => true;

        private IValueHandler GetHandler(Func<IValueHandler, bool> predicate) =>
            ValueHandlers.Values.OrderByDescending(x => x.Priority).FirstOrDefault(predicate) ?? new DefaultValueHandler();
    }
}
