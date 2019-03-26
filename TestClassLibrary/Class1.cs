using System;
using scheduller.api;

namespace TestClassLibrary
{
    public class Class1 : ITrigger
    {
        public void startRunner()
        {
            Console.Write("Test trigger worked");
        }
    }
}