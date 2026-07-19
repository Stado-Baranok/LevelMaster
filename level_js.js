// level_js.js — строительный уровень с калибровкой на JavaScript (Node.js + readline)

const fs = require('fs');
const readline = require('readline');

const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
    prompt: '> '
});

class App {
    constructor() {
        this.angle = 0;
        this.offset = 0;
        this.sensitivity = 0.5;
        this.calibrated = false;
        this.simulating = false;
        this.vertical = false;
        this.beep = true;
        this.configFile = 'level_config.json';
        this.loadConfig();
    }

    loadConfig() {
        try {
            const data = fs.readFileSync(this.configFile, 'utf8');
            const cfg = JSON.parse(data);
            this.offset = cfg.offset || 0;
            this.calibrated = cfg.calibrated || false;
            this.sensitivity = cfg.sensitivity || 0.5;
            this.vertical = cfg.vertical || false;
            this.beep = cfg.beep !== undefined ? cfg.beep : true;
        } catch (e) {}
    }

    saveConfig() {
        const cfg = {
            offset: this.offset,
            calibrated: this.calibrated,
            sensitivity: this.sensitivity,
            vertical: this.vertical,
            beep: this.beep
        };
        fs.writeFileSync(this.configFile, JSON.stringify(cfg, null, 2));
    }

    readAngle() {
        if (!this.simulating) {
            this.angle += Math.random() * 2 - 1;
            if (this.angle > 360) this.angle -= 360;
            if (this.angle < 0) this.angle += 360;
        }
    }

    calibrate() {
        console.log('Калибровка... (имитация)');
        let sum = 0;
        for (let i = 0; i < 10; i++) {
            sum += Math.random() * 4 - 2;
        }
        const avg = sum / 10;
        this.offset = -avg;
        this.calibrated = true;
        console.log(`Калибровка завершена. Смещение: ${this.offset.toFixed(2)}°`);
        this.saveConfig();
    }

    reset() {
        this.offset = 0;
        this.calibrated = false;
        this.angle = 0;
        console.log('Сброшено');
    }

    toggleSimulation() {
        this.simulating = !this.simulating;
        console.log(`Симуляция ${this.simulating ? 'включена' : 'выключена'}`);
        if (this.simulating) {
            this.simInterval = setInterval(() => {
                this.angle = (this.angle + 0.2) % 360;
                this.display();
            }, 50);
        } else {
            clearInterval(this.simInterval);
        }
    }

    toggleVertical() {
        this.vertical = !this.vertical;
        console.log(`Режим: ${this.vertical ? 'вертикаль' : 'горизонталь'}`);
    }

    toggleBeep() {
        this.beep = !this.beep;
        console.log(`Звук ${this.beep ? 'включён' : 'выключен'}`);
    }

    setSensitivity(val) {
        val = parseFloat(val);
        if (isNaN(val)) { console.log('Неверное число'); return; }
        if (val < 0.1) val = 0.1;
        this.sensitivity = val;
        console.log(`Чувствительность: ${this.sensitivity.toFixed(1)}°`);
    }

    display() {
        let displayAngle = (this.angle + this.offset) % 360;
        if (displayAngle > 180) displayAngle -= 360;
        let deviation = displayAngle;
        if (this.vertical) deviation -= 90;
        if (deviation > 180) deviation -= 360;
        // Рисуем шкалу
        const bar = this.drawBar(deviation);
        if (this.vertical) {
            console.log(`Отклонение от вертикали: ${deviation.toFixed(1)}° ${bar}`);
        } else {
            console.log(`Наклон: ${deviation.toFixed(1)}° ${bar}`);
        }
        if (this.beep && Math.abs(deviation) < this.sensitivity) {
            process.stdout.write('\x07');
        }
    }

    drawBar(deg) {
        const width = 30;
        const center = width / 2;
        let pos = Math.round(deg / 30 * center + center);
        if (pos < 0) pos = 0;
        if (pos >= width) pos = width - 1;
        let bar = '░'.repeat(width).split('');
        // Цвет через ANSI
        let color = '\x1b[32m'; // зелёный
        if (Math.abs(deg) >= this.sensitivity && Math.abs(deg) < this.sensitivity*3) {
            color = '\x1b[33m'; // жёлтый
        } else if (Math.abs(deg) >= this.sensitivity*3) {
            color = '\x1b[31m'; // красный
        }
        bar[pos] = '█';
        return color + bar.join('') + '\x1b[0m';
    }

    interactive() {
        console.log('📏 LevelMaster — JavaScript Edition');
        console.log('Команды: read, calibrate, reset, sim, vertical, beep, sens <val>, info, exit');
        rl.prompt();

        rl.on('line', (line) => {
            const parts = line.trim().split(' ');
            const cmd = parts[0];
            const arg = parts.slice(1).join(' ');
            switch (cmd) {
                case 'read':
                    this.readAngle();
                    this.display();
                    break;
                case 'calibrate':
                    this.calibrate();
                    this.display();
                    break;
                case 'reset':
                    this.reset();
                    this.display();
                    break;
                case 'sim':
                    this.toggleSimulation();
                    break;
                case 'vertical':
                    this.toggleVertical();
                    this.display();
                    break;
                case 'beep':
                    this.toggleBeep();
                    break;
                case 'sens':
                    this.setSensitivity(arg);
                    break;
                case 'info':
                    this.display();
                    break;
                case 'exit':
                    this.saveConfig();
                    console.log('До свидания!');
                    rl.close();
                    return;
                default:
                    console.log('Неизвестная команда');
            }
            rl.prompt();
        }).on('close', () => {
            process.exit(0);
        });
    }
}

const app = new App();
app.interactive();
