using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace EmbeddedLockTest
{
    //Demonstrate how .NET/C# locks can cause deadlocks across multiple threads.
    internal class Program
    {
        private static object m_LockObject = new object(); //The actual lock object
        private const Int32 EMBEDDED_METHOD_CALL_SLEEP_TIME_MS = 1000; //Sleeps added to make the demos more clear
        private const Int32 TEST_COMPLETE_DELAY_MS = 2000; //Sleeps added to make the demos more clear
        
        static void Main(string[] args)
        {
            var prog = new Program();
            prog.SingleThreadTest(); //fine
            GetUserPressAKeyToContinue();

            prog.MultipleThreadsWithNoJoin(); //fine
            GetUserPressAKeyToContinue();

            prog.MultipleThreadsWithNoJoinAndException(); //fine
            GetUserPressAKeyToContinue();

            prog.MultipleThreadsWithJoin(); //deadlock
            GetUserPressAKeyToContinue();
        }

        //This works fine, a lock block on the same thread flows right through.
        private void SingleThreadTest()
        {
            Log("Test 1: Single thread ---");
            Log("Aquiring lock from main method...");
            lock (m_LockObject)
            {
                EmbeddedMethodCallWithLock(null);
            }

            Log("Main method released lock.");
        }

        //This also works fine, method call on the separate thread has to wait for the main thread's lock block to complete.
        private void MultipleThreadsWithNoJoin()
        {
            Log("Test 2: Multiple threads, don't wait for thread to finish ---");

            Log("Aquiring lock from main method...");
            lock (m_LockObject)
            {
                var separateThread = new Thread(EmbeddedMethodCallWithLock);
                separateThread.Start(false);
                Log(string.Format("Sleeping for: {0} ms. in main method call.", EMBEDDED_METHOD_CALL_SLEEP_TIME_MS));
                Thread.Sleep(EMBEDDED_METHOD_CALL_SLEEP_TIME_MS);
            }

            Log("Main method released lock.");
        }

        //This also works fine, confirms that throwing an exception will correctly give up the lock.
        private void MultipleThreadsWithNoJoinAndException()
        {
            Log("Test 3: Multiple threads, but throw exception in main thread's lock---");

            try
            {
                Log("Aquiring lock from main method...");
                lock (m_LockObject)
                {
                    var separateThread = new Thread(EmbeddedMethodCallWithLock);
                    separateThread.Start(false);
                    Log(string.Format("Sleeping for: {0} ms. in main method call.", EMBEDDED_METHOD_CALL_SLEEP_TIME_MS));
                    Thread.Sleep(EMBEDDED_METHOD_CALL_SLEEP_TIME_MS);
                    var exceptionInfo = "Simulating an exception from within lock block in main method...";
                    Log(exceptionInfo);
                    throw new Exception(exceptionInfo);
                }
            }
            catch (Exception)
            {
                Log("Main method caught exception here.");
            }

            Log("Main method released lock.");
        }

        //Deadlock scenario, main method keeps lock while waiting for separate thread to complete.  Separate thread can't complete because it's trying to get the same lock.
        private void MultipleThreadsWithJoin()
        {
            Log("Test 4: Multiple threads, wait for thread to finish via join ---");

            try
            {
                Log("Aquiring lock from main method...");
                lock (m_LockObject)
                {
                    var separateThread = new Thread(EmbeddedMethodCallWithLock);
                    separateThread.Start(false);                 
                    Log(string.Format("Sleeping for: {0} ms. in main method call.", EMBEDDED_METHOD_CALL_SLEEP_TIME_MS));
                    Thread.Sleep(EMBEDDED_METHOD_CALL_SLEEP_TIME_MS);

                    Log("Main thread is now attempting to join with the method call thread, it can't.");
                    Log("Deadlock achieved.");
                    separateThread.Join();
                    Log("You will never see this. :(");
                }
            }
            catch (Exception)
            {
                Log("Main method caught exception here.");
            }

            Log("Main method released lock.");
        }

        private void EmbeddedMethodCallWithLock(object o)
        {
            Log("Called method acquiring lock...");
            lock (m_LockObject)
            {
                Log("Called method acquired lock, sleeping...");
                Thread.Sleep(EMBEDDED_METHOD_CALL_SLEEP_TIME_MS);
                Log("Called method awake.");
            }

            Log("Called method completed okay.");
        }

        //Helper methods
        private static void GetUserPressAKeyToContinue()
        {
            Thread.Sleep(TEST_COMPLETE_DELAY_MS);
            Console.WriteLine();
            Console.WriteLine("Press a key to continue...");
            Console.ReadKey();
            Console.WriteLine();
        }

        private static void Log(string message)
        {
            Console.WriteLine(string.Format("{0} [Thread: {1}] - {2}", DateTime.Now.ToString("H:mm:ss:fff"), Thread.CurrentThread.ManagedThreadId.ToString("00"), message));
        }
    }
}