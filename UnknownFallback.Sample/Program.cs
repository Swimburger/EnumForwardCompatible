using System.Text.Json;
using EnumForwardCompatible.UnknownFallback;

Console.WriteLine("Serialize of a known enum value:");
var dog = new Dog
{
    Breed = DogBreed.Beagle
};

Console.WriteLine(dog.ToJson());

Console.WriteLine("Serialize of a known enum value:");
dog = Dog.FromJson(dog.ToJson());
Console.WriteLine(dog.ToJson());

var dogJson = JsonSerializer.Serialize(new
{
    breed = "labrador"
});
Console.WriteLine("Deserialize of an unknown enum value:");
dog = Dog.FromJson(dogJson);
Console.WriteLine(dog.ToJson());

switch (dog.Breed)
{
    case DogBreed.Beagle:
        Console.WriteLine("It's a beagle!");
        break;
    case DogBreed.Bulldog:
        Console.WriteLine("It's a bulldog!");
        break;
    case DogBreed.Poodle:
        Console.WriteLine("It's a poodle!");
        break;
    case DogBreed.Unknown:
        Console.WriteLine("It's an unknown breed!");
        break;
}