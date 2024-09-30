using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnumForwardCompatible.UnknownFallback;

public static class JsonOptions
{
    public static JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
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
        Assert.That(dog.Breed, Is.EqualTo(EnumUtils.ToEnum<DogBreed>(CurrentEnumValue)));
    }


    [Test]
    public void ShouldParseNewEnum()
    {
        var dog = JsonSerializer.Deserialize<Dog>(JsonWithNewEnum, JsonOptions.Options);
        Assert.That(dog, Is.Not.Null);
        Assert.That(EnumUtils.ToEnumString(dog.Breed), Is.EqualTo(EnumUtils.ToEnumString(DogBreed.Unknown)));
    }

    [Test]
    public void ShouldSerializeExistingEnum()
    {
        var json = JsonSerializer.SerializeToElement(new Dog(EnumUtils.ToEnum<DogBreed>(CurrentEnumValue)),
            JsonOptions.Options);
        TestContext.WriteLine("Serialized JSON: \n" + json);
        var breed = json.GetProperty("breed").GetString();
        Assert.That(breed, Is.Not.Null);
        Assert.That(breed, Is.EqualTo(CurrentEnumValue));
    }

    [Test]
    [Ignore(
        "You cannot implement this test because the new enum value is not supported by the current implementation.")]
    public void ShouldSerializeNewEnum()
    {
        throw new Exception(
            "You cannot implement this test because the new enum value is not supported by the current implementation.");
    }
}

public record Dog()
{
    public Dog(DogBreed breed) : this()
    {
        Breed = breed;
    }

    [JsonPropertyName("breed")] public DogBreed Breed { get; set; }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonOptions.Options);
    }

    public static Dog FromJson(string json)
    {
        return JsonSerializer.Deserialize<Dog>(json, JsonOptions.Options);
    }
}

[JsonConverter(typeof(StringEnumSerializer<DogBreed>))]
public enum DogBreed
{
    // explicitly set 0 to Unknown
    // 0 value is used as default
    [EnumMember(Value = "unknown")] Unknown = 0,
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