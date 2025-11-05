package SummitDemo.SpringApiService;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.context.properties.EnableConfigurationProperties;

@SpringBootApplication
@EnableConfigurationProperties
public class SpringApiServiceApplication {

	public static void main(String[] args) {
		SpringApplication.run(SpringApiServiceApplication.class, args);
	}

}
