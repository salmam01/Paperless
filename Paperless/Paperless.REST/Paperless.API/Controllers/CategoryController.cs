using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Paperless.API.DTOs;
using Paperless.BL.Exceptions;
using Paperless.BL.Models.Domain;
using Paperless.BL.Services.Categories;

namespace Paperless.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(
            ICategoryService categoryService,
            IMapper mapper,
            ILogger<CategoryController> logger
        ) {
            _categoryService = categoryService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet(Name = "GetCategories")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<CategoryDTO>>> GetAll()
        {
            _logger.LogInformation(
                "Incoming GET /category from {ip}.",
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            try
            {
                IEnumerable<Category> categories = await _categoryService.GetCategoriesAsync();
                IEnumerable<CategoryDTO> categoriesDTO = _mapper.Map<IEnumerable<CategoryDTO>>(categories);

                _logger.LogInformation("GET /category retrieved {count} categories successfully.", categoriesDTO.Count());
                return Ok(categoriesDTO);
            }
            catch (ServiceException ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /category failed in {layer} Layer due to {reason}.",
                    "GET", "Business", GetExceptionMessage(ex.Type)
                );
                return Problem(
                    detail: ex.Message,
                    statusCode: GetHttpStatusCode(ex.Type),
                    title: GetExceptionMessage(ex.Type)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /category failed in {layer} Layer due to {reason}.",
                    "GET", "API", ex.Message
                );
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
        }

        [HttpGet("{id}", Name = "GetCategory")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Get(string id)
        {
            _logger.LogInformation(
                "Incoming GET /category/{id} from {ip}.",
                id,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            if (!Guid.TryParse(id, out Guid guid))
            {
                _logger.LogWarning("GET /category/{id} failed due to an invalid ID.", id);
                return BadRequest("Invalid ID.");
            }

            try
            {
                Category category = await _categoryService.GetCategoryAsync(guid);
                CategoryDTO categoryDTO = _mapper.Map<CategoryDTO>(category);

                _logger.LogInformation("GET /category/{id} retrieved category successfully.", id);
                return Ok(categoryDTO);
            }
            catch (ServiceException ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /category/{id} failed in {layer} Layer due to {reason}.",
                    "GET", id, "Business", GetExceptionMessage(ex.Type)
                );
                return Problem(
                    detail: ex.Message,
                    statusCode: GetHttpStatusCode(ex.Type),
                    title: GetExceptionMessage(ex.Type)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /category/{id} failed in {layer} Layer due to {reason}.",
                    "GET", id, "API", ex.Message
                );
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
        }

        [HttpPost(Name = "PostCategory")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CategoryDTO>> Post(string name)
        {

            if (string.IsNullOrEmpty(name))
            {
                _logger.LogWarning("POST /category failed due to empty or invalid file format.");
                return BadRequest("Empty or invalid name.");
            }

            _logger.LogInformation(
                "Incoming POST /category from {ip}. Category Name: {name}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                name
            );

            try
            {
                CategoryDTO categoryDTO = new(Guid.NewGuid(), name);

                if (categoryDTO == null)
                    return BadRequest("Empty or invalid category.");

                Category category = _mapper.Map<Category>(categoryDTO);
                await _categoryService.AddCategoryAsync(category);

                _logger.LogInformation("POST /category uploaded category with ID {Id} successfully.", categoryDTO.Id);
                return CreatedAtAction(nameof(Get), new { id = categoryDTO.Id }, categoryDTO);
            }
            catch (ServiceException ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /category failed in {layer} Layer due to {reason}.",
                    "POST", "Business", GetExceptionMessage(ex.Type)
                );
                return Problem(
                    detail: ex.Message,
                    statusCode: GetHttpStatusCode(ex.Type),
                    title: GetExceptionMessage(ex.Type)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{method} /category failed in {layer} Layer due to {reason}.",
                    "POST",
                    "API",
                    ex.Message
                );
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error"
                );
            }
        }


        private int GetHttpStatusCode(ExceptionType type)
        {
            switch (type)
            {
                case ExceptionType.Validation: return StatusCodes.Status400BadRequest;
                case ExceptionType.Internal: return StatusCodes.Status500InternalServerError;
                default: return StatusCodes.Status500InternalServerError;
            }
        }

        private string GetExceptionMessage(ExceptionType type)
        {
            switch (type)
            {
                case ExceptionType.Validation:
                    return "Validation Failed";
                case ExceptionType.Internal:
                    return "Internal Server Error";
                default:
                    return "Unknown Error";
            }
        }
    }
}
