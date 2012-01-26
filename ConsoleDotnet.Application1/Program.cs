using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleDotnet.Application1 {

public class PassingEventArgs : System.ComponentModel.CancelEventArgs
{
    public string Value {get;set;}
}

class Pet
{
    private String _msg;

    public Pet()
    {
        System.Console.WriteLine("Pet created.");
    }

    public Pet(String msg)
    {
        _msg = msg;
        System.Console.WriteLine(_msg);
    }

    public void Say()
    {
        System.Console.WriteLine("Dog said \"I'm a pet\".");
    }
}

class Dog : Pet
{
    public Dog() : base("From derived.")
    {
        System.Console.WriteLine("Dog created");
    }

    public new void Say()
    {
        PassingEventArgs evt = new PassingEventArgs();
        evt.Value = "From Say()";

        OnBeginEvent(evt);

        System.Console.WriteLine("Dog said \"Woof\"!");

        OnEndEvent(evt);
    }

    public delegate void BeginDelegate(Pet sender, PassingEventArgs evt);
    public delegate void EndDelegate(Pet sender, PassingEventArgs evt);

    public event BeginDelegate BeginEvent;
    public event EndDelegate EndEvent;

    protected virtual void OnBeginEvent(PassingEventArgs evt)
    {
        if(BeginEvent != null) {
            BeginEvent(this, evt);
        }
    }

    protected virtual void OnEndEvent(PassingEventArgs evt)
    {
        if(EndEvent != null) {
            EndEvent(this, evt);
        }
    }
}
class Program
{
    static void Main(string[] args)
    {
        System.Diagnostics.Debug.WriteLine("Program Started");

        Dog rocky = new Dog();

        rocky.BeginEvent += new Dog.BeginDelegate(delegate(Pet sender, PassingEventArgs evt) {
            System.Diagnostics.Debug.WriteLine("BeginEvent EventArgs:" + evt.Value);
            //System.Diagnostics.Debugger.Break();
            evt.Value = "From BeginEvent()";
            System.Diagnostics.Debug.WriteLine("BeginEvent");
        });

        rocky.EndEvent += new Dog.EndDelegate(delegate(Pet sender, PassingEventArgs evt) {
            System.Diagnostics.Debug.WriteLine("EndEvent EventArgs:" + evt.Value);
            //System.Diagnostics.Debugger.Break();
            System.Diagnostics.Debug.WriteLine("EndEvent");
        });

        rocky.Say();
        ((Pet)rocky).Say();

        Console.WriteLine("\n========== ==========\n");

        String s = "Hello World";
        Console.WriteLine(s);

        // Prevent console window from closing.
        // Alternative Solution: Debug > Start Without Debugging Ctrl+F5
        Console.WriteLine("Press return/enter key to exit.");
        Console.Read();
    }
}

} // END namespace ConsoleDotnet.Application1