using EnumValueOneOf;

Console.WriteLine("Serialize of a known enum value:");
var dog = new Dog
{
    Breed = DogBreed.Beagle
};

Console.WriteLine(dog.ToJson());

Console.WriteLine("Serialize of a known enum value:");
dog = Dog.FromJson(dog.ToJson());
Console.WriteLine(dog.ToJson());

dog = new Dog
{
    Breed = "labrador"
};

Console.WriteLine(dog.ToJson());

Console.WriteLine("Deserialize of an unknown enum value:");
dog = Dog.FromJson(dog.ToJson());
Console.WriteLine(dog.ToJson());

if (dog.Breed.IsEnum)
{
    switch (dog.Breed.AsEnum)
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
        default:
            Console.WriteLine($"It's an unknown {dog.Breed}!");
            break;
    }
}
else
{
    Console.WriteLine($"It's an unknown breed: {dog.Breed}");
}