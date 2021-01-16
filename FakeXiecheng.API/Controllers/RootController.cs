using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeXiecheng.API.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FakeXiecheng.API.Controllers
{
    [Route("api")]
    [ApiController]
    public class RootController:ControllerBase
    {
        [HttpGet(Name = "GetRoot")]
        public IActionResult GetRoot()
        {
            var links = new List<LinkDto>();
            //self link
            links.Add(
                new LinkDto(
                    Url.Link("GetRoot", null),
                    "self",
                    "GET"));
            //1st order links "GET api/touristRoutes"
            links.Add(
                new LinkDto(
                    Url.Link("GetTouristRoutes",null),
                    "get_tourist_routes",
                    "GET"));

            //1st order links "POST api/touristRoutes"
            links.Add(
                new LinkDto(
                    Url.Link("CreateTouristRoute",null),
                    "create_tourist_route",
                    "POST"));

            //1st order link shoppingCarts "GET api/shoppingCart"
            links.Add(
                new LinkDto(
                    Url.Link("GetShoppingCart", null),
                    "get_shopping_cart",
                    "GET"));

            //1st order link Order "GET api/orders
            links.Add(
                new LinkDto(
                    Url.Link("GetOrders", null),
                    "get_orders",
                    "GET"));
            return Ok(links);
        }
    }
}
