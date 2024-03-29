using System; 
using System.Reflection; 
using System.IO; class Program 
{
    public static void Main() {
        object watcher = new FileSystemWatcher()
            .GetType () 
            .GetField ("watcher", BindingFlags.NonPublic | BindingFlags.Static) 
            .GetValue (null); 
            Console.WriteLine ("Your file system watcher is: {0}", 
            watcher != null 
            ? watcher.GetType ().FullName
            : "unknown");
    }
}
