using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using FakeXiecheng.API.Dtos;
using FakeXiecheng.API.Helpers;
using FakeXiecheng.API.Models;
using FakeXiecheng.API.ResourceParameters;
using FakeXiecheng.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Net.Http.Headers;

namespace FakeXiecheng.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TouristRoutesController: ControllerBase
    {
        private readonly  ITouristRouteRepository _touristRouteRepository;
        private readonly IMapper _mapper;
        private readonly IUrlHelper _urlHelper;
        private readonly IPropertyMappingService _propertyMappingService;
        public TouristRoutesController(
            ITouristRouteRepository touristRouteRepository,
            IMapper mapper,
            IUrlHelperFactory urlHelperFactory,
            IActionContextAccessor actionContextAccessor,
            IPropertyMappingService propertyMappingService)
        {
            _touristRouteRepository = touristRouteRepository;
            _mapper = mapper;
            _urlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
            _propertyMappingService = propertyMappingService;
        }

        private string GenerateTouristRouteResourceURL(
            TouristRouteResourceParamaters parameters,
            PaginationResourceParamaters parameters2,
            ResourceUriType type
        )
        {
            return type switch
            {
                ResourceUriType.PreviousPage => _urlHelper.Link("GetTouristRoutes",
                    new
                    {
                        fields = parameters.Fields,
                        orderBy = parameters.OrderBy,
                        keyword = parameters.Keyword,
                        rating = parameters.Rating,
                        pageNumber = parameters2.PageNumber - 1,
                        pageSize = parameters2.PageSize
                    }),
                ResourceUriType.NextPage => _urlHelper.Link("GetTouristRoutes",
                    new
                    {
                        fields = parameters.Fields,
                        orderBy = parameters.OrderBy,
                        keyword = parameters.Keyword,
                        rating = parameters.Rating,
                        pageNumber = parameters2.PageNumber + 1,
                        pageSize = parameters2.PageSize
                    }),
                _ => _urlHelper.Link("GetTouristRoutes", 
                    new
                    {
                        fields = parameters.Fields,
                        orderBy = parameters.OrderBy,
                        keyword = parameters.Keyword,
                        rating = parameters.Rating,
                        pageNumber = parameters2.PageNumber,
                        pageSize = parameters2.PageSize
                    }),
            };
        }
        //api/touristRoutes?keyword=input content
        //1. application/json -> tourist route resource 
        //2. application/vnd.{company name}.hateoas+json
        //3. application/vnd.guangqing.touristRoute.simplify+json
        //4. application/vnd.guangqing.touristRoute.simplify.hateoas+json
        [Produces(
            "application/json",
            "application/vnd.guangqing.hateoas+json",
            "application/vnd.guangqing.touristRoute.simplify+json",
            "application/vnd.guangqing.touristRoute.simplify.hateoas+json"
            )]
        [HttpGet(Name = "GetTouristRoutes")]
        [HttpHead]
        public async Task<IActionResult> GetTouristRoutes(
            [FromQuery] TouristRouteResourceParamaters paramaters,
            [FromQuery] PaginationResourceParamaters paramaters2,
            [FromHeader(Name = "Accept")] string mediaType
            //[FromQuery] string keyword,
            //string rating //lessThan, largerThan, equalTo
            )//fromquery or frombody
        {
            if (!MediaTypeHeaderValue.TryParse(
                mediaType, out MediaTypeHeaderValue parsedMediatype))
            {
                return BadRequest();
            }

            if (!_propertyMappingService.IsMappingExists<TouristRouteDto, TouristRoute>(paramaters.OrderBy))
            {
                return BadRequest("please input the right orderby parameters");
            }

            if (!_propertyMappingService.IsPropertiesExists<TouristRouteDto>(paramaters.Fields))
            {
                return BadRequest("please type in the correct parameters");
            }

            var touristRoutesFromRepo 
                = await _touristRouteRepository
                .GetTouristRoutesAsync(
                    paramaters.Keyword, 
                    paramaters.RatingOperator, 
                    paramaters.RatingValue,
                    paramaters2.PageSize,
                    paramaters2.PageNumber,
                    paramaters.OrderBy);
            if (touristRoutesFromRepo == null || !touristRoutesFromRepo.Any())
            {
                return NotFound("no tourist route");
            }

            var touristRouteDto = _mapper.Map<IEnumerable<TouristRouteDto>>(touristRoutesFromRepo);

            var previousPageLink = touristRoutesFromRepo.HasPrevious
                ? GenerateTouristRouteResourceURL(
                    paramaters, paramaters2, ResourceUriType.PreviousPage)
                : null;

            var nextPageLink = touristRoutesFromRepo.HasNext
                ? GenerateTouristRouteResourceURL(
                    paramaters, paramaters2, ResourceUriType.NextPage)
                : null;
            //x-pagination
            var paginationMetadata = new
            {
                previousPageLink,
                nextPageLink,
                totalCount = touristRoutesFromRepo.TotalCount,
                pageSize = touristRoutesFromRepo.PageSize,
                currentPage = touristRoutesFromRepo.CurrentPage,
                totalPages = touristRoutesFromRepo.TotalPages
            };

            Response.Headers.Add("x-pagination", 
                Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

            bool isHateoas = parsedMediatype.SubTypeWithoutSuffix
                .EndsWith("hateoas", StringComparison.InvariantCultureIgnoreCase);

            var primaryMediaType = isHateoas
                ? parsedMediatype.SubTypeWithoutSuffix
                    .Substring(0, parsedMediatype.SubTypeWithoutSuffix.Length - 8)
                : parsedMediatype.SubTypeWithoutSuffix;
            
            //var shapedDtoList = touristRouteDto.ShapeData(paramaters.Fields);
            IEnumerable<object> touristRoutesDto;
            IEnumerable<ExpandoObject> shapedDtoList;

            if (primaryMediaType == "vnd.guangqing.touristRoute.simplify")
            {
                touristRoutesDto = _mapper
                    .Map<IEnumerable<TouristRouteSimplifyDto>>(touristRoutesFromRepo);

                shapedDtoList = ((IEnumerable<TouristRouteSimplifyDto>) touristRoutesDto)
                    .ShapeData(paramaters.Fields);
            }
            else
            {
                touristRoutesDto = _mapper
                    .Map<IEnumerable<TouristRouteDto>>(touristRoutesFromRepo);
                shapedDtoList = ((IEnumerable<TouristRouteDto>) touristRoutesDto)
                    .ShapeData(paramaters.Fields);
            }

            if (isHateoas)
            {
                var linkDto = CreateLinksForTouristRouteList(paramaters, paramaters2);

                var shapedDtoWithLinkList = shapedDtoList.Select(t =>
                {
                    var touristRouteDictionary = t as IDictionary<string, object>;
                    var links = CreateLinkForTouristRoute((Guid)touristRouteDictionary["Id"], null);

                    touristRouteDictionary.Add("links", links);
                    return touristRouteDictionary;
                });
                var result = new
                {
                    value = shapedDtoWithLinkList,
                    links = linkDto
                };

                return Ok(result);
            }

            return Ok(shapedDtoList);
        }

        private IEnumerable<LinkDto> CreateLinksForTouristRouteList(
            TouristRouteResourceParamaters parameters,
            PaginationResourceParamaters parameters2
            )
        {
            var links = new List<LinkDto>();
            // ADD self
            links.Add(new LinkDto(
                GenerateTouristRouteResourceURL(parameters, parameters2, ResourceUriType.CurrentPage),
                "self",
                "GET"
                ));
            // "api/touristRoutes"
            // create touristRoute
            links.Add(new LinkDto(
                Url.Link("CreateTouristRoute", null),
                "create_tourist_route",
                "POST"));

            return links;
        }

        [HttpGet("{touristRouteId:Guid}", Name = "GetTouristRouteById")]
        [HttpHead]
        public async Task<IActionResult> GetTouristRouteByIdAsync(
            Guid touristRouteId,
            string fields)
        {
            var touristRouteFromRepo = await _touristRouteRepository.GetTouristRouteAsync(touristRouteId);
            if (touristRouteFromRepo == null)
            {
                return NotFound($"tourist route {touristRouteId} cannot find");
            }

            var touristRouteDto = _mapper.Map<TouristRouteDto>(touristRouteFromRepo);

            //return Ok(touristRouteDto.ShapeData(fields));
            var linkDtos = CreateLinkForTouristRoute(touristRouteId, fields);
            var result = touristRouteDto.ShapeData(fields)
                as IDictionary<string, object>;
            result.Add("links", linkDtos);
            return Ok(result);
        }

        private IEnumerable<LinkDto> CreateLinkForTouristRoute(
            Guid touristRouteId,
            string fields)
        {
            var links = new List<LinkDto>();

            links.Add(
                new LinkDto(
                    Url.Link("GetTouristRouteById", new {touristRouteId, fields}),
                    "self",
                    "GET"));
            //PUT
            links.Add(
                new LinkDto(
                    Url.Link("PartiallyUpdateTouristRoute", new {touristRouteId}),
                    "partially_update",
                    "PATCH"
                    ));
            //DELETE
            links.Add(
                new LinkDto(
                    Url.Link("DeleteTouristRoute", new {touristRouteId}),
                    "delete",
                    "DELETE"));
            // GET TOURISTROUTE PIC
            links.Add(new LinkDto(
                Url.Link("GetPictureListForTouristRoute", new {touristRouteId}),
                "get_picture",
                "GET"));
            //ADD NEW PIC
            links.Add(new LinkDto(
                Url.Link("CreateTouristRoutePicture", new {touristRouteId}),
                "create_picture",
                "POST"));

            return links;
        }

        [HttpPost(Name = "CreateTouristRoute")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTouristRouteAsync([FromBody] TouristRouteForCreationDto touristRouteForCreationDto)
        {
            var touristRouteModel = _mapper.Map<TouristRoute>(touristRouteForCreationDto);
            _touristRouteRepository.AddTouristRoute(touristRouteModel);
            await _touristRouteRepository.SaveAsync();

            var touristRouteToReturn = _mapper.Map<TouristRouteDto>(touristRouteModel);

            var links = CreateLinkForTouristRoute(touristRouteModel.Id, null);
            var result = touristRouteToReturn.ShapeData(null)
                as IDictionary<string, object>;
                
            result.Add("links", links);

            return CreatedAtRoute(
                "GetTouristRouteById",
                new {touristRouteId = result["Id"]},
                result);
        }

        [HttpPut("{touristRouteId}", Name = "UpdateTouristRoute")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTouristRoute(
            [FromRoute] Guid touristRouteId,
            [FromBody] TouristRouteForUpdateDto touristRouteForUpdateDto
            )
        {
            if (! await _touristRouteRepository.TouristRouteExistsAsync(touristRouteId))
            {
                return NotFound("Cannot find the tourist route");
            }

            var touristRouteFromRepo = await _touristRouteRepository.GetTouristRouteAsync(touristRouteId);
            //1. 映射Dto
            //2. 更新dto
            //3. 映射model
            _mapper.Map(touristRouteForUpdateDto, touristRouteFromRepo);
            await _touristRouteRepository.SaveAsync();

            return NoContent();
        }

        [HttpPatch("{touristRouteId}",Name = "PartiallyUpdateTouristRoute")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PartiallyUpdateTouristRoute(
            [FromRoute]Guid touristRouteId,
            [FromBody] JsonPatchDocument<TouristRouteForUpdateDto> patchDocument)
        {
            if (!await _touristRouteRepository.TouristRouteExistsAsync(touristRouteId))
            {
                return NotFound("tourist route cannot find");
            }

            var touristRouteFromRepo =await _touristRouteRepository.GetTouristRouteAsync(touristRouteId);
            var touristRouteToPatch = _mapper.Map<TouristRouteForUpdateDto>(touristRouteFromRepo);
            patchDocument.ApplyTo(touristRouteToPatch, ModelState);
            if (TryValidateModel(touristRouteToPatch))
            {
                return ValidationProblem(ModelState);
            }
            _mapper.Map(touristRouteToPatch, touristRouteFromRepo);
            await _touristRouteRepository.SaveAsync();

            return NoContent();
        }
        [HttpDelete("{touristRouteId}", Name = "DeleteTouristRoute")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTouristRoute([FromRoute] Guid touristRouteId)
        {
            if (! await _touristRouteRepository.TouristRouteExistsAsync(touristRouteId))
            {
                return NotFound("the tourist route does not exist");
            }

            var touristRoute = await _touristRouteRepository.GetTouristRouteAsync(touristRouteId);
            _touristRouteRepository.DeleteTouristRoute(touristRoute);
            await _touristRouteRepository.SaveAsync();

            return NoContent();
        }

        [HttpDelete("({touristIDs})")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteByIDs(
            [ModelBinder(BinderType = typeof(ArrayModelBinder))]
            [FromRoute] IEnumerable<Guid> touristIDs)
        {
            if (touristIDs == null)
            {
                return BadRequest();
            }

            var touristRoutesFromRepo =await _touristRouteRepository.GetTouristRoutesByIDListAsync(touristIDs);
            _touristRouteRepository.DeleteTouristRoutes(touristRoutesFromRepo);
            await _touristRouteRepository.SaveAsync();

            return NoContent();
        }
    }
}
 