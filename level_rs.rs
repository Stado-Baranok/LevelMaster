// level_rs.rs — строительный уровень с калибровкой на Rust (консоль + termion)

use rand::Rng;
use serde::{Deserialize, Serialize};
use serde_json;
use std::fs;
use std::io::{self, Write, BufRead};
use std::f64::consts::PI;
use termion::{color, style};

#[derive(Serialize, Deserialize)]
struct Config {
    offset: f64,
    calibrated: bool,
    sensitivity: f64,
    vertical: bool,
    beep: bool,
}

struct App {
    angle: f64,
    offset: f64,
    sensitivity: f64,
    calibrated: bool,
    simulating: bool,
    vertical: bool,
    beep: bool,
    config_file: String,
    rng: rand::ThreadRng,
}

impl App {
    fn new() -> Self {
        App {
            angle: 0.0,
            offset: 0.0,
            sensitivity: 0.5,
            calibrated: false,
            simulating: false,
            vertical: false,
            beep: true,
            config_file: "level_config.json".to_string(),
            rng: rand::thread_rng(),
        }
    }

    fn load_config(&mut self) {
        if let Ok(data) = fs::read_to_string(&self.config_file) {
            if let Ok(cfg) = serde_json::from_str::<Config>(&data) {
                self.offset = cfg.offset;
                self.calibrated = cfg.calibrated;
                self.sensitivity = cfg.sensitivity;
                self.vertical = cfg.vertical;
                self.beep = cfg.beep;
            }
        }
    }

    fn save_config(&self) {
        let cfg = Config {
            offset: self.offset,
            calibrated: self.calibrated,
            sensitivity: self.sensitivity,
            vertical: self.vertical,
            beep: self.beep,
        };
        if let Ok(data) = serde_json::to_string_pretty(&cfg) {
            let _ = fs::write(&self.config_file, data);
        }
    }

    fn read_angle(&mut self) {
        if !self.simulating {
            self.angle += self.rng.gen_range(-1.0..1.0);
            if self.angle > 360.0 { self.angle -= 360.0; }
            if self.angle < 0.0 { self.angle += 360.0; }
        }
    }

    fn calibrate(&mut self) {
        println!("Калибровка... (имитация)");
        let mut sum = 0.0;
        for _ in 0..10 {
            sum += self.rng.gen_range(-2.0..2.0);
        }
        let avg = sum / 10.0;
        self.offset = -avg;
        self.calibrated = true;
        println!("Калибровка завершена. Смещение: {:.2}°", self.offset);
        self.save_config();
    }

    fn reset(&mut self) {
        self.offset = 0.0;
        self.calibrated = false;
        self.angle = 0.0;
        println!("Сброшено");
    }

    fn toggle_simulation(&mut self) {
        self.simulating = !self.simulating;
        if self.simulating {
            println!("Симуляция включена");
        } else {
            println!("Симуляция выключена");
        }
    }

    fn toggle_vertical(&mut self) {
        self.vertical = !self.vertical;
        println!("Режим: {}", if self.vertical { "вертикаль" } else { "горизонталь" });
    }

    fn toggle_beep(&mut self) {
        self.beep = !self.beep;
        println!("Звук: {}", if self.beep { "включён" } else { "выключен" });
    }

    fn set_sensitivity(&mut self, val: f64) {
        let val = if val < 0.1 { 0.1 } else { val };
        self.sensitivity = val;
        println!("Чувствительность: {:.1}°", self.sensitivity);
    }

    fn display(&self) {
        let mut display_angle = self.angle + self.offset;
        display_angle %= 360.0;
        if display_angle > 180.0 { display_angle -= 360.0; }
        let mut deviation = display_angle;
        if self.vertical { deviation -= 90.0; }
        if deviation > 180.0 { deviation -= 360.0; }
        // Рисуем шкалу
        let bar = self.draw_bar(deviation);
        if self.vertical {
            println!("{}Отклонение от вертикали: {:.1}° {}{}", color::Fg(color::Green), deviation, bar, style::Reset);
        } else {
            println!("{}Наклон: {:.1}° {}{}", color::Fg(color::Green), deviation, bar, style::Reset);
        }
        if self.beep && deviation.abs() < self.sensitivity {
            print!("\x07");
            io::stdout().flush().unwrap();
        }
    }

    fn draw_bar(&self, deg: f64) -> String {
        const WIDTH: usize = 30;
        let center = (WIDTH / 2) as f64;
        let pos = (deg / 30.0 * center + center) as usize;
        let pos = if pos >= WIDTH { WIDTH - 1 } else if pos < 0 { 0 } else { pos };
        let mut bar = vec!['░'; WIDTH];
        let color = if deg.abs() < self.sensitivity {
            color::Fg(color::Green)
        } else if deg.abs() < self.sensitivity * 3.0 {
            color::Fg(color::Yellow)
        } else {
            color::Fg(color::Red)
        };
        bar[pos] = '█';
        format!("{}{}{}", color, bar.iter().collect::<String>(), style::Reset)
    }

    fn interactive(&mut self) {
        let stdin = io::stdin();
        let mut reader = stdin.lock();
        println!("{}📏 LevelMaster — Rust Edition{}", color::Fg(color::Cyan), style::Reset);
        println!("Команды: read, calibrate, reset, sim, vertical, beep, sens <val>, info, exit");
        loop {
            print!("{}> {} ", color::Fg(color::Yellow), style::Reset);
            io::stdout().flush().unwrap();
            let mut line = String::new();
            if reader.read_line(&mut line).is_err() { break; }
            let line = line.trim();
            if line.is_empty() { continue; }
            let parts: Vec<&str> = line.splitn(2, ' ').collect();
            let cmd = parts[0];
            let arg = if parts.len() > 1 { parts[1] } else { "" };
            match cmd {
                "read" => {
                    self.read_angle();
                    self.display();
                }
                "calibrate" => {
                    self.calibrate();
                    self.display();
                }
                "reset" => {
                    self.reset();
                    self.display();
                }
                "sim" => self.toggle_simulation(),
                "vertical" => {
                    self.toggle_vertical();
                    self.display();
                }
                "beep" => self.toggle_beep(),
                "sens" => {
                    if let Ok(val) = arg.parse::<f64>() {
                        self.set_sensitivity(val);
                    } else {
                        println!("Неверное число");
                    }
                }
                "info" => self.display(),
                "exit" => {
                    self.save_config();
                    println!("До свидания!");
                    break;
                }
                _ => println!("Неизвестная команда"),
            }
        }
    }
}

fn main() {
    let mut app = App::new();
    app.load_config();
    app.interactive();
}
