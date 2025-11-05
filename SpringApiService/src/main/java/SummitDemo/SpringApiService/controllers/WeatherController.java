package SummitDemo.SpringApiService.controllers;

import SummitDemo.SpringApiService.config.WeatherOptions;
import SummitDemo.SpringApiService.models.WeatherForecast;
import org.springframework.http.MediaType;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.time.LocalDate;
import java.util.List;
import java.util.Random;
import java.util.stream.IntStream;

@RequestMapping
@RestController
public class WeatherController {

    private final WeatherOptions weatherOptions;
    private final Random random = new Random();

    public WeatherController(WeatherOptions weatherOptions) {
        this.weatherOptions = weatherOptions;
    }

    @GetMapping(value = "/weatherforecast", produces = MediaType.APPLICATION_JSON_VALUE)
    public WeatherForecast[] getWeatherForecast() {
        List<String> summaries = weatherOptions.getSummaries();
        final List<String> summariesList = summaries;
        return IntStream.range(1, 6)
                .mapToObj(i -> new WeatherForecast(
                        LocalDate.now().plusDays(i),
                        random.nextInt(75) - 20,
                        summariesList.get(random.nextInt(summariesList.size()))))
                .toArray(WeatherForecast[]::new);
    }

}
