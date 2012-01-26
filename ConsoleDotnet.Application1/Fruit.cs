using System;
using System.Text;

namespace ConsoleDotnet.Application1 {

class Fruit
{
    public enum KindOfFruit {
        ORANGE,
        APPLE
    };

    private KindOfFruit _kindOfFruit;

    public Fruit() {}

    public Fruit(KindOfFruit f) {this._kindOfFruit = f;}

    public KindOfFruit Kind() {return _kindOfFruit;}

    public int Partition(ref Fruit[] aFruit)
    {
        // Declare and init prerequisites.
        bool repeat = true;
        bool hasDifferences = false;
        int length = aFruit.Length;
        int x, y;
        Fruit xFruit, yFruit; 

        // Using a simple algorithm bubble sort.
        while(repeat) {
            repeat = false;
            for(int i = 0;i < length - 1;i += 1) {
                // Convert enum to integer.
                x = (int)aFruit[i].Kind();
                y = (int)aFruit[i + 1].Kind();
                // Validate order.
                if(x > y) {
                    // Convert integer to enum.
                    xFruit = new Fruit((KindOfFruit)x);
                    yFruit = new Fruit((KindOfFruit)y);
                    // Swap.
                    aFruit[i] = yFruit;
                    aFruit[i + 1] = xFruit;
                    // Iterate primary loop.
                    repeat = true;
                }
                if(x != y && hasDifferences == false) {
                    hasDifferences = true;
                }
                //Console.WriteLine(x + " : " + y + " : " + hasDifferences.ToString());
            }

            //Console.WriteLine("Has differences : " + hasDifferences.ToString());

            if(hasDifferences == false) {return 0;}

            /*
            var sb1 = new StringBuilder();
            for(int i = 0;i < length;i += 1) {
                sb1.Append(", ");
                sb1.Append(aFruit[i].Kind().ToString());
            }
            Console.WriteLine(sb1.Remove(0, 2).ToString());
            */
        }

        return 1;
    }
}

class Program {
    static void Main(string[] args) {
        Fruit f1 = new Fruit();
        Fruit[] fruits = new Fruit[] {
            new Fruit(Fruit.KindOfFruit.ORANGE),
            new Fruit(Fruit.KindOfFruit.ORANGE),
            new Fruit(Fruit.KindOfFruit.APPLE),
            new Fruit(Fruit.KindOfFruit.APPLE),
            new Fruit(Fruit.KindOfFruit.ORANGE),
            new Fruit(Fruit.KindOfFruit.APPLE),
            new Fruit(Fruit.KindOfFruit.ORANGE)
        };
        int state = f1.Partition(ref fruits);

        var sb1 = new StringBuilder();
        for(int i = 0;i < fruits.Length;i += 1) {
            sb1.Append(", ");
            sb1.Append(fruits[i].Kind().ToString());
        }
        Console.WriteLine(sb1.Remove(0, 2).ToString());

        Console.WriteLine("Returned Value : " + state);

        Console.WriteLine("\n========== ========== ========== ==========\n");

        // Prevent console window from closing.
        // Alternative Solution: Debug > Start Without Debugging Ctrl+F5
        Console.WriteLine("Press return/enter key to exit.");
        Console.Read();
    }
}

}
