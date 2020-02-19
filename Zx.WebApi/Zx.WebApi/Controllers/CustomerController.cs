using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Zx.Core.Services;
using Zx.Core.Runtime;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Zx.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CustomerController : ControllerBase
    {
        // GET: api/<controller>
        [HttpGet]
        public ActionResult Get()
        {
            return Ok(Singleton.Instance.Get());
        }

        // GET api/<controller>/GUID
        [HttpGet("{id}")]
        public ActionResult Get(string id)
        {
            return Ok(Singleton.Instance.Get(id));
        }

        // POST api/<controller>
        [HttpPost]
        public ActionResult Post([FromBody]JsonElement value)
        {
            var json = JsonSerializer.Serialize(value);
            Singleton.Instance.Post(json);
            return Ok();
        }

        // PUT api/<controller>/GUID
        [HttpPut("{id}")]
        public ActionResult Put(string id, [FromBody]JsonElement value)
        {
            var json = JsonSerializer.Serialize(value);
            Singleton.Instance.Put(id, json);
            return Ok();
        }

        // DELETE api/<controller>/GUID
        [HttpDelete("{id}")]
        public ActionResult Delete(string id)
        {
            Singleton.Instance.Delete(id);
            return Ok();
        }

        public sealed class Singleton
        {
            private static readonly Lazy<Singleton>
                lazy =
                new Lazy<Singleton>
                    (() => new Singleton());

            public static Singleton Instance { get { return lazy.Value; } }
            private ICustomerService _customerService;

            private Singleton()
            {
                var runtime = new RuntimeSettings(Core.Runtime.Environment.Production);
                var connString = runtime.Config(ConfigKeys.ConnString);
                _customerService = new CustomerService(connString);
            }

            public IEnumerable<object> Get()
            {
                return _customerService.Get();
            }

            public object Get(string id)
            {
                return _customerService.Get(id);
            }

            public void Post(string data)
            {
                _customerService.Create(data);
            }

            public void Put(string id, string data)
            {
                _customerService.Update(id, data);
            }

            public void Delete(string id)
            {
                _customerService.Delete(id);
            }
        }
    }
}
