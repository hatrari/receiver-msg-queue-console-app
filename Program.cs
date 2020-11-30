using System;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.ServiceBus;
using System.Threading;
using System.IO;

namespace receiver_msg_queue_console
{
  class Program
  {
    private static string _connectionString;
    private static string _queuename;
    private static IQueueClient _client;
    public static IConfigurationRoot Configuration { get; set; }
    static void Main(string[] args)
    {
      var builder = ConfigureBuilder();
      _connectionString = builder["connectionstring"];
      _queuename = builder["queuename"];

      MainAsync().GetAwaiter().GetResult();
    }

    private static IConfigurationRoot ConfigureBuilder()
    {
      return new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .Build();
    }
    
    static async Task MainAsync()
    {
        _client = new QueueClient(_connectionString, _queuename);
        RegisterOnMessageHandlerAndReceiveMessages();
        Console.WriteLine("Press any key after getting all messages.");
        Console.ReadKey();

        await _client.CloseAsync();
    }

    static async Task ProcessMessagesAsync(Message message, CancellationToken token)
    {
      Console.WriteLine($"Received message: #{message.SystemProperties.SequenceNumber} Body: {Encoding.UTF8.GetString(message.Body)}");
      await _client.CompleteAsync(message.SystemProperties.LockToken);
    }

    static void RegisterOnMessageHandlerAndReceiveMessages()
    {
      var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
      {
          MaxConcurrentCalls = 1,
          AutoComplete = false
      };

      _client.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
    }

    static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
    {
      Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
      var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
      Console.WriteLine("Exception context for troubleshooting:");
      Console.WriteLine($"- Endpoint: {context.Endpoint}");
      Console.WriteLine($"- Entity Path: {context.EntityPath}");
      Console.WriteLine($"- Executing Action: {context.Action}");
      return Task.CompletedTask;
    }

  }
}
