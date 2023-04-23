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

 private readonly string _docPath;

 private readonly string _hostingName; 

    public bookingController(ILogger<bookingController> logger, IConfiguration config)
    {
        _docPath = config["DocPath"];
        _logger = logger;
        _hostingName = config["hostingname"];
        var hostName = System.Net.Dns.GetHostName();
var ips = System.Net.Dns.GetHostAddresses(hostName);
var _ipaddr = ips.First().MapToIPv4().ToString();
_logger.LogInformation(1, $"Taxabooking responding from {_ipaddr}");
    }

    //Note: If I had to make more methods I most likely would have made a "service" component to handle rabbitmq related things 

    //method to post bookings
   [HttpPost(Name = "PostBooking")]
    public void Post([FromBody]BookingDTO booking) 
    {
        //send info to logger
        _logger.LogInformation("Method 'PostBooking' from service TaxaBooking called at {DT}",  
        DateTime.UtcNow.ToLongTimeString()); 
         
        //convert from BookingDTO to PlanDTO 
        PlanDTO newPlan = new PlanDTO();
        newPlan.CustomerName = booking.CustomerName;
        newPlan.start = booking.start;
        newPlan.slut = booking.slut;
        newPlan.id = booking.id;
        
        //Create connection to rabbitmq server
        var factory = new ConnectionFactory { HostName = "_hostname"  };
        using var connection = factory.CreateConnection();
        //use connection to make channel
        using var channel = connection.CreateModel();
        _logger.LogInformation($"Making queue using {factory.HostName}");
        //use channel to declare queue
        channel.QueueDeclare(queue: "Planning Service",
                     durable: false,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);
        //convert from PlanDTO to JSON-string 
        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(newPlan));
        //publishes the JSON-string to the channel
        channel.BasicPublish(exchange: string.Empty,
                     routingKey: "Planning Service",
                     basicProperties: null,
                     body: body);
                     
    
    }
    //method to get the plan.csv
    [HttpGet(Name = "getPlanFile")]
     public FileStreamResult GetFile()
        {
         _logger.LogInformation("Method 'GetPlanFile' from service TaxaBooking called at {DT}",  
        DateTime.UtcNow.ToLongTimeString()); 
            //define path to find file
            var physicalPath = _docPath;
            _logger.LogInformation($"fetching files from {_docPath}");
            //read the file
            var pdfBytes = System.IO.File.ReadAllBytes(Path.Combine(_docPath,"Plan.csv"));
            //stream result so it can be returned
            var ms = new MemoryStream(pdfBytes);
            return File(ms, "text/csv");
        }
}
    

   


