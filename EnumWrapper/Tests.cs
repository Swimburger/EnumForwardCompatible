using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnumWrapper;

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };
}

public class Tests
{
    private static readonly DogBreed CurrentEnumValue = DogBreed.Beagle;
    private static readonly DogBreed NewEnumValue = DogBreed.Custom("labrador");

    private static readonly string JsonWithCurrentEnum =
        $$"""
          {
              "breed": "{{CurrentEnumValue}}"
          }
          """;

    private static readonly string JsonWithNewEnum =
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
        Assert.That(dog.Breed, Is.EqualTo(CurrentEnumValue));
    }


    [Test]
    public void ShouldParseNewEnum()
    {
        var dog = JsonSerializer.Deserialize<Dog>(JsonWithNewEnum, JsonOptions.Options);
        Assert.That(dog, Is.Not.Null);
        Assert.That(dog.Breed, Is.EqualTo(NewEnumValue));
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
    [JsonPropertyName("breed")] public DogBreed Breed { get; set; }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonOptions.Options);
    }
    
    public static Dog FromJson(string json)
    {
        return JsonSerializer.Deserialize<Dog>(json, JsonOptions.Options) ?? throw new InvalidOperationException();
    }
}

[JsonConverter(typeof(DogBreedEnumConverter))]
public sealed class DogBreed : JsonEnum
{
    public static readonly DogBreed Beagle = new("beagle");
    public static readonly DogBreed Bulldog = new("bulldog");
    public static readonly DogBreed Poodle = new("poodle");

    private DogBreed(string value) : base(value)
    {
    }

    public static DogBreed Custom(string value) => new(value);
}

public class JsonEnum : IEquatable<string>, IEquatable<JsonEnum>
{
    protected internal JsonEnum(string value) => Value = value;
    public string Value { get; protected internal set; }

    public override string ToString()
    {
        return Value;
    }

    public bool Equals(JsonEnum other)
    {
        return Value == other.Value;
    }

    public bool Equals(string? other)
    {
        return Value.Equals(other);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((JsonEnum)obj);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(JsonEnum value1, JsonEnum value2)
        => value1.Equals(value2);

    public static bool operator !=(JsonEnum value1, JsonEnum value2)
        => !(value1 == value2);

    public static bool operator ==(JsonEnum value1, string value2)
        => value1.Value.Equals(value2);

    public static bool operator !=(JsonEnum value1, string value2)
        => !value1.Value.Equals(value2);
}

internal class DogBreedEnumConverter : JsonEnumConverter<DogBreed>
{
    public override DogBreed CreateJsonEnum(string value)
    {
        return DogBreed.Custom(value);
    }
}

internal abstract class JsonEnumConverter<T> : JsonConverter<T> where T : JsonEnum
{
    public abstract T CreateJsonEnum(string value);
    
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue =
            reader.GetString()
            ?? throw new Exception("The JSON value could not be read as a string.");
        return CreateJsonEnum(stringValue);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}