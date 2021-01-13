using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.Configuration.Conventions;
using FakeXiecheng.API.Dtos;
using FakeXiecheng.API.Helpers;
using FakeXiecheng.API.Models;
using FakeXiecheng.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FakeXiecheng.API.Controllers
{
    [ApiController]
    [Route("api/shoppingCart")]
    public class ShoppingCartController: ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITouristRouteRepository _touristRouteRepository;
        private readonly IMapper _mapper;

        public ShoppingCartController(
            IHttpContextAccessor httpContextAccessor,
            ITouristRouteRepository touristRouteRepository,
            IMapper mapper)
        {
            _httpContextAccessor = httpContextAccessor;
            _touristRouteRepository = touristRouteRepository;
            _mapper = mapper;
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetShoppingCart()
        {
            //1. get the user
            var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

            //2. get shopping cart with userId
            var shoppingCart = await _touristRouteRepository.GetShoppingCartByUserId(userId);
 
            return Ok(_mapper.Map<ShoppingCartDto>(shoppingCart));
        }

        [HttpPost("items")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> AddShoppingCartItem(
            [FromBody] AddShoppingCartItemDto addShoppingCartItemDto
        )
        {
            //1.get the current user
            var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

            //2 use the userId to get shoppingcart
            var shoppingCart = await _touristRouteRepository.GetShoppingCartByUserId(userId);
            //3. create line item
            var touristRoute =
                await _touristRouteRepository.GetTouristRouteAsync(addShoppingCartItemDto.TouristRouteId);

            if (touristRoute == null)
            {
                return NotFound("tourist route does not exist");
            }

            var lineItem = new LineItem()
            {
                TouristRouteId = addShoppingCartItemDto.TouristRouteId,
                ShoppingCartId = shoppingCart.Id,
                OriginalPrice = touristRoute.OriginalPrice,
                DiscountPresent = touristRoute.DiscountPrice
            };

            //4. add lineItem, save data to database
            await _touristRouteRepository.AddShoppingCartItem(lineItem);
            await _touristRouteRepository.SaveAsync();

            return Ok(_mapper.Map<ShoppingCartDto>(shoppingCart));
        }

        [HttpDelete("items/{itemId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> DeleteShoppingCartItem([FromRoute] int itemId)
        {
            //1. get lineItem data
            var lineItem = await _touristRouteRepository.GetShoppingCartItemByItemId(itemId);
            if (lineItem == null)
            {
                return NotFound("can not find item in the shopping cart");
            }
            _touristRouteRepository.DeleteShoppingCartItem(lineItem);
            await _touristRouteRepository.SaveAsync();

            return NoContent();
        }

        [HttpDelete("items/({itemIDs})")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> RemoveShoppingCartItems(
            [ModelBinder(BinderType = typeof(ArrayModelBinder))]
            [FromRoute] IEnumerable<int> itemIDs
            )
        {
            var lineItems = await _touristRouteRepository
                .GetShoppingCartsByIdListAsync(itemIDs);
            _touristRouteRepository.DeleteShoppingCartItems(lineItems);
            await _touristRouteRepository.SaveAsync();

            return NoContent();
        }

        [HttpPost("checkout")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> Checkout()
        {
            //1.get current user
            var userId = _httpContextAccessor
                .HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            //2. get shopping cart items via userId
            var shoppingCart = await _touristRouteRepository.GetShoppingCartByUserId(userId);
            //3. create order
            var order = new Order()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                State = OrderStateEnum.Pending,
                OrderItems = shoppingCart.ShoppingCartItems,
                CreateDateUTC = DateTime.UtcNow
            };
            shoppingCart.ShoppingCartItems = null;
            //4. save data
            await _touristRouteRepository.AddOrderAsync(order);
            await _touristRouteRepository.SaveAsync();
            //5. return 
            return Ok(_mapper.Map<OrderDto>(order));
        }
    }
}
