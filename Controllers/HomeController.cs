using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Project01_movie_lease_system.Models;
using Project01_movie_lease_system.Repositories;

namespace Project01_movie_lease_system.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly MovieRepository _movieRepository;
    private readonly ReviewRepository _reviewRepository;

    public HomeController(ILogger<HomeController> logger,
    MovieRepository movieRepository, ReviewRepository reviewRepository)
    {
        _logger = logger;
        _movieRepository = movieRepository;
        _reviewRepository = reviewRepository;
    }

    public IActionResult Index()
    {
        var viewModel = new HomeIndexViewModel
        {
            FeaturedMovies = _movieRepository.GetFeaturedMovies(3).ToList(),
            MovieTypes = Enum.GetValues(typeof(MovieType)).Cast<MovieType>().ToList(),
            RecentReviews = _reviewRepository.GetRecentReviews(5).ToList()
        };
        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }
    public IActionResult Service()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
