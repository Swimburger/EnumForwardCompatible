using EnumWrapper;

Console.WriteLine("Serialize of a known enum value:");

var dog = new Dog
{
    Breed = DogBreed.Beagle
};

Console.WriteLine(dog.ToJson());

Console.WriteLine("Serialize of a known enum value:");
dog = Dog.FromJson(dog.ToJson());
Console.WriteLine(dog.ToJson());

Console.WriteLine("Serialize of an unknown enum value:");
dog = new Dog
{
    Breed = DogBreed.Custom("labrador")
};

Console.WriteLine(dog.ToJson());

Console.WriteLine("Deserialize of an unknown enum value:");
dog = Dog.FromJson(dog.ToJson());
Console.WriteLine(dog.ToJson());

switch (dog.Breed.Value)
{
    case "beagle":
        Console.WriteLine("It's a beagle!");
        break;
    case "bulldog":
        Console.WriteLine("It's a bulldog!");
        break;
    case "poodle":
        Console.WriteLine("It's a poodle!");
        break;
    default:
        Console.WriteLine("It's an unknown breed!");
        break;
}

if (dog.Breed == DogBreed.Beagle)
    Console.WriteLine("It's a beagle!");
else if (dog.Breed == DogBreed.Bulldog)
    Console.WriteLine("It's a bulldog!");
else if (dog.Breed == DogBreed.Poodle)
    Console.WriteLine("It's a poodle!");
else
    Console.WriteLine("It's an unknown breed!");