using System;
using System.Windows.Forms;

namespace GoonWarfareX;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        
        try 
        {
            Console.WriteLine("--- SYSTEM START ---");
            Application.Run(new Form1());
        }
        catch (Exception ex)
        {
            // If the app crashes, THIS will tell us why in the terminal
            Console.WriteLine("🚨 CRITICAL ERROR DURING STARTUP:");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            Console.ReadLine(); // Keeps the terminal open so you can read the error
        }
    }    
}