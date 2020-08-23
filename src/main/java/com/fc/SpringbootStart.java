package com.fc;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.autoconfigure.jdbc.DataSourceAutoConfiguration;

import springfox.documentation.swagger2.annotations.EnableSwagger2;


/**
 * 项目启动方法
 * @author fuce
 *
 */
@SpringBootApplication(exclude = DataSourceAutoConfiguration.class)
public class SpringbootStart {

    public static void main(String[] args) {

        SpringApplication.run(SpringbootStart.class, args);
        System.out.println("           ____      ________  ,---------.      .-''-.   .-------.     .--.      .--.    ____     ,---.  ,---.     .-''-.            _______     _______       .-'''-.          \n" + 
        		"         .'  __ `.  |        | \\          \\   .'_ _   \\  |  _ _   \\    |  |_     |  |  .'  __ `.  |   /  |   |   .'_ _   \\          \\  ____  \\  \\  ____  \\    / _     \\         \n" + 
        		"        /   '  \\  \\ |   .----'  `--.  ,---'  / ( ` )   ' | ( ' )  |    | _( )_   |  | /   '  \\  \\ |  |   |  .'  / ( ` )   '         | |    \\ |  | |    \\ |   (`' )/`--'         \n" + 
        		"        |___|  /  | |  _|____      |   \\    . (_ o _)  | |(_ o _) /    |(_ o _)  |  | |___|  /  | |  | _ |  |  . (_ o _)  |         | |____/ /  | |____/ /  (_ o _).            \n" + 
        		"           _.-`   | |_( )_   |     :_ _:    |  (_,_)___| | (_,_).' __  | (_,_) \\ |  |    _.-`   | |  _( )_  |  |  (_,_)___|         |   _ _ '.  |   _ _ '.   (_,_). '.          \n" + 
        		"        .'   _    | (_ o._)__|     (_I_)    '  \\   .---. |  |\\ \\  |  | |  |/    \\|  | .'   _    | \\ (_ o._) /  '  \\   .---.         |  ( ' )  \\ |  ( ' )  \\ .---.  \\  :         \n" + 
        		"        |  _( )_  | |(_,_)        (_(=)_)    \\  `-'    / |  | \\ `'   / |  '  /\\  `  | |  _( )_  |  \\ (_,_) /    \\  `-'    /         | (_{;}_) | | (_{;}_) | \\    `-'  |         \n" + 
        		"        \\ (_ o _) / |   |          (_I_)      \\       /  |  |  \\    /  |    /  \\    | \\ (_ o _) /   \\     /      \\       /          |  (_,_)  / |  (_,_)  /  \\       /          \n" + 
        		"         '.(_,_).'  '---'          '---'       `'-..-'   ''-'   `'-'   `---'    `---`  '.(_,_).'     `---`        `'-..-'           /_______.'  /_______.'    `-...-'           \n" + 
        		"                                                                                                                                                                                ");
    }
}
