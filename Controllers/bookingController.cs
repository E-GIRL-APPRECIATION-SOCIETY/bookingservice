using Microsoft.AspNetCore.Mvc;
using TaxaService.DTO;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Newtonsoft.Json;
namespace TaxaService.Controllers;

[ApiController]
[Route("[controller]")]
public class bookingController : ControllerBase
{
 private readonly ILogger<bookingController> _logger;

    public bookingController(ILogger<bookingController> logger)
    {
        _logger = logger;
    }

    //Note: If I had to make more methods I most likely would have made a "service" component to handle rabbitmq related things 

    //method to post bookings
   [HttpPost(Name = "PostBooking")]
    public void Post([FromBody]BookingDTO booking) 
    {
        //send info to logger
        _logger.LogInformation("Method 'Post' from service TaxaBooking called at {DT}",  
        DateTime.UtcNow.ToLongTimeString()); 
        //convert from BookingDTO to PlanDTO 
        PlanDTO newPlan = new PlanDTO();
        newPlan.CustomerName = booking.CustomerName;
        newPlan.start = booking.start;
        newPlan.slut = booking.slut;
        newPlan.id = booking.id;
        //Create connection to rabbitmq server
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        //use connection to make channel
        using var channel = connection.CreateModel();
        //use channel to declare queue
        channel.QueueDeclare(queue: "plansender",
                     durable: false,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);
        //convert from PlanDTO to JSON-string 
        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(newPlan));
        //publishes the JSON-string to the channel
        channel.BasicPublish(exchange: string.Empty,
                     routingKey: "plansender",
                     basicProperties: null,
                     body: body);
    
    }
    //method to get the plan.csv
    [HttpGet(Name = "getPlanFile")]
     public FileStreamResult GetFile()
        {
            //define path to find file
            var physicalPath = $"./Content/plan.csv";
            //read the file
            var pdfBytes = System.IO.File.ReadAllBytes(physicalPath);
            //stream result so it can be returned
            var ms = new MemoryStream(pdfBytes);
            return new FileStreamResult(ms,"csv");
        }
}
    

   


