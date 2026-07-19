// level_go.go — строительный уровень с калибровкой на Go (консоль)

package main

import (
	"bufio"
	"encoding/json"
	"fmt"
	"io/ioutil"
	"math"
	"math/rand"
	"os"
	"strconv"
	"strings"
	"time"
)

type Config struct {
	Offset      float64 `json:"offset"`
	Calibrated  bool    `json:"calibrated"`
	Sensitivity float64 `json:"sensitivity"`
	Vertical    bool    `json:"vertical"`
	Beep        bool    `json:"beep"`
}

type App struct {
	angle       float64
	offset      float64
	sensitivity float64
	calibrated  bool
	simulating  bool
	vertical    bool
	beep        bool
	configFile  string
}

func NewApp() *App {
	return &App{
		sensitivity: 0.5,
		beep:        true,
		configFile:  "level_config.json",
	}
}

func (a *App) loadConfig() {
	data, err := ioutil.ReadFile(a.configFile)
	if err == nil {
		var cfg Config
		err = json.Unmarshal(data, &cfg)
		if err == nil {
			a.offset = cfg.Offset
			a.calibrated = cfg.Calibrated
			a.sensitivity = cfg.Sensitivity
			a.vertical = cfg.Vertical
			a.beep = cfg.Beep
		}
	}
}

func (a *App) saveConfig() {
	cfg := Config{Offset: a.offset, Calibrated: a.calibrated, Sensitivity: a.sensitivity, Vertical: a.vertical, Beep: a.beep}
	data, _ := json.MarshalIndent(cfg, "", "  ")
	ioutil.WriteFile(a.configFile, data, 0644)
}

func (a *App) readAngle() {
	if !a.simulating {
		a.angle += rand.Float64()*2 - 1
		if a.angle > 360 {
			a.angle -= 360
		}
		if a.angle < 0 {
			a.angle += 360
		}
	}
}

func (a *App) calibrate() {
	fmt.Println("Калибровка... (имитация)")
	sum := 0.0
	for i := 0; i < 10; i++ {
		sum += rand.Float64()*4 - 2
	}
	avg := sum / 10
	a.offset = -avg
	a.calibrated = true
	fmt.Printf("Калибровка завершена. Смещение: %.2f°\n", a.offset)
	a.saveConfig()
}

func (a *App) reset() {
	a.offset = 0.0
	a.calibrated = false
	a.angle = 0.0
	fmt.Println("Сброшено")
}

func (a *App) toggleSimulation() {
	a.simulating = !a.simulating
	if a.simulating {
		fmt.Println("Симуляция включена")
	} else {
		fmt.Println("Симуляция выключена")
	}
}

func (a *App) toggleVertical() {
	a.vertical = !a.vertical
	fmt.Println("Режим:", map[bool]string{true: "вертикаль", false: "горизонталь"}[a.vertical])
}

func (a *App) toggleBeep() {
	a.beep = !a.beep
	fmt.Println("Звук:", map[bool]string{true: "включён", false: "выключен"}[a.beep])
}

func (a *App) setSensitivity(val float64) {
	if val < 0.1 {
		val = 0.1
	}
	a.sensitivity = val
	fmt.Printf("Чувствительность: %.1f°\n", a.sensitivity)
}

func (a *App) display() {
	displayAngle := math.Mod(a.angle+a.offset, 360)
	if displayAngle > 180 {
		displayAngle -= 360
	}
	deviation := displayAngle
	if a.vertical {
		deviation -= 90
	}
	if deviation > 180 {
		deviation -= 360
	}
	// Визуализация шкалы
	bar := drawLevelBar(deviation, a.sensitivity)
	if a.vertical {
		fmt.Printf("Отклонение от вертикали: %.1f° %s\n", deviation, bar)
	} else {
		fmt.Printf("Наклон: %.1f° %s\n", deviation, bar)
	}
	if a.beep && math.Abs(deviation) < a.sensitivity {
		fmt.Print("\a") // системный beep
	}
}

func drawLevelBar(deg, sens float64) string {
	const width = 30
	center := width / 2
	pos := int(deg/30*float64(center) + float64(center))
	if pos < 0 {
		pos = 0
	}
	if pos >= width {
		pos = width - 1
	}
	bar := make([]rune, width)
	for i := range bar {
		bar[i] = '░'
	}
	// Определяем цвет (в консоли - через ANSI)
	color := "\033[32m" // зелёный
	if math.Abs(deg) >= sens && math.Abs(deg) < sens*3 {
		color = "\033[33m" // жёлтый
	} else if math.Abs(deg) >= sens*3 {
		color = "\033[31m" // красный
	}
	bar[pos] = '█'
	return color + string(bar) + "\033[0m"
}

func (a *App) interactive() {
	scanner := bufio.NewScanner(os.Stdin)
	fmt.Println("📏 LevelMaster — Go Edition")
	fmt.Println("Команды: read, calibrate, reset, sim, vertical, beep, sens <val>, info, exit")
	for {
		fmt.Print("> ")
		if !scanner.Scan() {
			break
		}
		line := strings.TrimSpace(scanner.Text())
		if line == "" {
			continue
		}
		parts := strings.SplitN(line, " ", 2)
		cmd := parts[0]
		arg := ""
		if len(parts) > 1 {
			arg = parts[1]
		}
		switch cmd {
		case "read":
			a.readAngle()
			a.display()
		case "calibrate":
			a.calibrate()
			a.display()
		case "reset":
			a.reset()
			a.display()
		case "sim":
			a.toggleSimulation()
		case "vertical":
			a.toggleVertical()
			a.display()
		case "beep":
			a.toggleBeep()
		case "sens":
			if val, err := strconv.ParseFloat(arg, 64); err == nil {
				a.setSensitivity(val)
			} else {
				fmt.Println("Неверное число")
			}
		case "info":
			a.display()
		case "exit":
			a.saveConfig()
			fmt.Println("До свидания!")
			return
		default:
			fmt.Println("Неизвестная команда")
		}
	}
}

func main() {
	rand.Seed(time.Now().UnixNano())
	app := NewApp()
	app.loadConfig()
	app.interactive()
}
