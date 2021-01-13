using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using FakeXiecheng.API.Dtos;
using FakeXiecheng.API.ResourceParameters;
using FakeXiecheng.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FakeXiecheng.API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController: ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITouristRouteRepository _touristRouteRepository;
        private readonly IMapper _mapper;
        private readonly IHttpClientFactory _httpClientFactory;

        public OrdersController(
            IHttpContextAccessor httpContextAccessor,
            ITouristRouteRepository touristRouteRepository,
            IMapper mapper,
            IHttpClientFactory httpClientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _touristRouteRepository = touristRouteRepository;
            _mapper = mapper;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetOrders(
            [FromQuery] PaginationResourceParamaters parameters
            )
        {
            //1.get the current user
            var userId = _httpContextAccessor
                .HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            //2. user id get the order records
            var orders = await _touristRouteRepository
                .GetOrdersByUserId(userId, parameters.PageSize, parameters.PageNumber);

            return Ok(_mapper.Map<IEnumerable<OrderDto>>(orders));
        }

        [HttpGet("{orderId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetOrderById([FromRoute] Guid orderId)
        {
            //1 get the current user
            var userId = _httpContextAccessor
                .HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

            var order = await _touristRouteRepository.GetOrderById(orderId);
            return Ok(_mapper.Map<OrderDto>(order));
        }

        [HttpPost("{orderId/placeOrder}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> PlaceOrder([FromRoute] Guid orderId)
        {
            //1 get the current user id
            var userId = _httpContextAccessor
                .HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            //2. starte deal with the pay
            var order = await _touristRouteRepository.GetOrderById(orderId);
            order.PaymentProcessing();
            await _touristRouteRepository.SaveAsync();
            //3 3rd party pay, wait the response
            var httpClient = _httpClientFactory.CreateClient();
            string url = @"http://123.56.149.216/api/FakePaymentProcess?icode={0}orderNumber={1}&returnFault={2}";
            var response = await httpClient.PostAsync(
                string.Format(url, "44DBB994365A8610", order.Id, false),
                null
            );
            //4 retrieve the pay result and info
            bool isApproved = false;
            string transactionMetadata = "";
            if (response.IsSuccessStatusCode)
            {
                transactionMetadata = await response.Content.ReadAsStringAsync();
                var jsonObject = (JObject)JsonConvert.DeserializeObject(transactionMetadata);
                isApproved = jsonObject["approved"].Value<bool>();
            }
            //5 if successful finish the order
            if (isApproved)
            {
                order.PaymentApprove();
            }
            else
            {
                order.PaymentReject();
            }

            order.TransactionMetadata = transactionMetadata;
            await _touristRouteRepository.SaveAsync();
            return Ok(_mapper.Map<OrderDto>(order));
        }
    }
}
