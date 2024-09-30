using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneOf;

namespace EnumValueOneOf;

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters =
        {
            // important, so that the enum conversion throws an exception if the value is not known
            // then the OneOfSerializer will try to cast to string instead
            new JsonStringEnumConverter(),
            new OneOfSerializer<EnumValue<DogBreed>>()
        }
    };
}

public class Tests
{
    private const string CurrentEnumValue = "beagle";
    private const string NewEnumValue = "labrador";

    private const string JsonWithCurrentEnum =
        $$"""
          {
              "breed": "{{CurrentEnumValue}}"
          }
          """;

    private const string JsonWithNewEnum =
        $$"""
          {
              "breed": "{{NewEnumValue}}"
          }
          """;

    [Test]
    public void ShouldParseExistingEnum()
    {
        var dog = JsonSerializer.Deserialize<Dog>(JsonWithCurrentEnum, JsonOptions.Options);
        Assert.That(dog, Is.Not.Null);
        Assert.That(dog.Breed == CurrentEnumValue, Is.True);
    }


    [Test]
    public void ShouldParseNewEnum()
    {
        var dog = JsonSerializer.Deserialize<Dog>(JsonWithNewEnum, JsonOptions.Options);
        Assert.That(dog, Is.Not.Null);
        Assert.That(dog.Breed == NewEnumValue, Is.True);
    }

    [Test]
    public void ShouldSerializeExistingEnum()
    {
        var json = JsonSerializer.SerializeToElement(new Dog { Breed = CurrentEnumValue },
            JsonOptions.Options);
        TestContext.WriteLine("Serialized JSON: \n" + json);
        var breed = json.GetProperty("breed").GetString();
        Assert.That(breed, Is.Not.Null);
        Assert.That(breed, Is.EqualTo(CurrentEnumValue));
    }

    [Test]
    public void ShouldSerializeNewEnum()
    {
        var json = JsonSerializer.SerializeToElement(new Dog { Breed = NewEnumValue },
            JsonOptions.Options);
        TestContext.WriteLine("Serialized JSON: \n" + json);
        var breed = json.GetProperty("breed").GetString();
        Assert.That(breed, Is.Not.Null);
        Assert.That(breed, Is.EqualTo(NewEnumValue));
    }
}

public record Dog()
{
    [JsonPropertyName("breed")] public EnumValue<DogBreed> Breed { get; set; }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonOptions.Options);
    }

    public static Dog FromJson(string json)
    {
        return JsonSerializer.Deserialize<Dog>(json, JsonOptions.Options);
    }
}

public class EnumValue<T> :
    OneOfBase<T, string>,
    IEquatable<object>
    where T : Enum
{
    internal EnumValue(T enumValue) : base(enumValue)
    {
    }

    internal EnumValue(string stringValue) : base(stringValue)
    {
    }

    public bool IsEnum => IsT0;
    public bool IsString => IsT1;

    public T AsEnum => AsT0;
    public string AsString => AsT1;
    public static explicit operator T(EnumValue<T> enumValue) => enumValue.AsEnum;
    public static explicit operator string(EnumValue<T> enumValue) => enumValue.AsString;

    public static implicit operator EnumValue<T>(T value) => new(value);
    public static implicit operator EnumValue<T>(string value) => new(value);

    public static bool operator ==(EnumValue<T> value1, EnumValue<T> value2)
        => value1.Equals(value2);

    public static bool operator !=(EnumValue<T> value1, EnumValue<T> value2)
        => !(value1 == value2);

    public static bool operator ==(EnumValue<T> value1, string value2)
        => value1.Equals(value2);

    public static bool operator !=(EnumValue<T> value1, string value2)
        => !(value1 == value2);

    public static bool operator ==(EnumValue<T> value1, T value2)
        => value1.Equals(value2);

    public static bool operator !=(EnumValue<T> value1, T value2)
        => !(value1 == value2);

    public override bool Equals(object? value)
    {
        return value switch
        {
            EnumValue<T> enumValue => Equals(enumValue),
            OneOfBase<T, string> oneOfEnumString => Equals(oneOfEnumString),
            T realEnum => Equals(realEnum),
            string stringValue => Equals(stringValue),
            _ => false
        };
    }

    public bool Equals(EnumValue<T>? value) => value is not null && value.Value.Equals(Value);

    public bool Equals(OneOfBase<T, string>? value) => value is not null && value.Value.Equals(Value);

    public bool Equals(T? value)
    {
        if (value is null) return false;
        if (IsString) return EnumUtils.ToEnumString(value).Equals(AsString);
        if (IsEnum) return value.Equals(AsEnum);
        throw new Exception("Unexpected enum value type");
    }

    public bool Equals(string? value)
    {
        if (value is null) return false;
        if (IsString) return value.Equals(AsString);
        if (IsEnum) return value.Equals(EnumUtils.ToEnumString(AsEnum));
        throw new Exception("Unexpected enum value type");
    }

    public override string ToString()
    {
        return IsEnum ? EnumUtils.ToEnumString(Value) : AsString;
    }
}

[JsonConverter(typeof(StringEnumSerializer<DogBreed>))]
public enum DogBreed
{
    [EnumMember(Value = "beagle")] Beagle,
    [EnumMember(Value = "bulldog")] Bulldog,
    [EnumMember(Value = "poodle")] Poodle
}

static class EnumUtils
{
    internal static string ToEnumString<T>(T type)
    {
        var enumType = typeof(T);
        var name = Enum.GetName(enumType, type);
        var enumMemberAttribute =
            ((EnumMemberAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(EnumMemberAttribute), true))
            .SingleOrDefault();
        if (enumMemberAttribute == null) throw new Exception("EnumMemberAttribute not found");
        return enumMemberAttribute.Value;
    }

    internal static T ToEnum<T>(string str)
    {
        var enumType = typeof(T);
        foreach (var name in Enum.GetNames(enumType))
        {
            var enumMemberAttribute =
                ((EnumMemberAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(EnumMemberAttribute), true))
                .SingleOrDefault();
            if (enumMemberAttribute == null) continue;
            if (enumMemberAttribute.Value == str) return (T)Enum.Parse(enumType, name);
        }

        //throw exception or whatever handling you want or
        return default(T);
    }
}

internal class StringEnumSerializer<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private readonly Dictionary<TEnum, string> _enumToString = new();
    private readonly Dictionary<string, TEnum> _stringToEnum = new();

    public StringEnumSerializer()
    {
        var type = typeof(TEnum);
        var values = Enum.GetValues(type);

        foreach (var value in values)
        {
            var enumValue = (TEnum)value;
            var enumMember = type.GetMember(enumValue.ToString())[0];
            var attr = enumMember
                .GetCustomAttributes(typeof(EnumMemberAttribute), false)
                .Cast<EnumMemberAttribute>()
                .FirstOrDefault();

            var stringValue =
                attr?.Value
                ?? value.ToString()
                ?? throw new Exception("Unexpected null enum toString value");

            _enumToString.Add(enumValue, stringValue);
            _stringToEnum.Add(stringValue, enumValue);
        }
    }

    public override TEnum Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var stringValue =
            reader.GetString()
            ?? throw new Exception("The JSON value could not be read as a string.");
        return _stringToEnum.TryGetValue(stringValue, out var enumValue) ? enumValue : default;
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(_enumToString[value]);
    }
}

internal class OneOfSerializer<TOneOf> : JsonConverter<TOneOf>
    where TOneOf : IOneOf
{
    public override TOneOf? Read(
        ref Utf8JsonReader reader,
        System.Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType is JsonTokenType.Null)
            return default;

        foreach (var (type, cast) in s_types)
        {
            try
            {
                var readerCopy = reader;
                var result = JsonSerializer.Deserialize(ref readerCopy, type, options);
                reader.Skip();
                return (TOneOf)cast.Invoke(null, [result])!;
            }
            catch (JsonException)
            {
            }
        }

        throw new JsonException(
            $"Cannot deserialize into one of the supported types for {typeToConvert}"
        );
    }

    private static readonly (System.Type type, MethodInfo cast)[] s_types = GetOneOfTypes();

    public override void Write(Utf8JsonWriter writer, TOneOf value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Value, options);
    }

    private static (System.Type type, MethodInfo cast)[] GetOneOfTypes()
    {
        var casts = typeof(TOneOf)
            .GetRuntimeMethods()
            .Where(m => m.IsSpecialName && m.Name == "op_Implicit")
            .ToArray();
        var type = typeof(TOneOf);
        while (type != null)
        {
            if (
                type.IsGenericType
                && (type.Name.StartsWith("OneOf`") || type.Name.StartsWith("OneOfBase`"))
            )
            {
                return type.GetGenericArguments()
                    .Select(t => (t, casts.First(c => c.GetParameters()[0].ParameterType == t)))
                    .ToArray();
            }

            type = type.BaseType;
        }

        throw new InvalidOperationException($"{typeof(TOneOf)} isn't OneOf or OneOfBase");
    }
}