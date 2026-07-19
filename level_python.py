# level_python.py — строительный уровень с калибровкой на Python (Tkinter)

import tkinter as tk
import math
import json
import os
import random
import time
import sys

try:
    import winsound
    SOUND_AVAILABLE = True
except:
    SOUND_AVAILABLE = False

class Level:
    def __init__(self, root):
        self.root = root
        self.root.title("📏 LevelMaster — Python")
        self.root.geometry("600x650")
        self.angle = 0.0          # угол наклона в градусах
        self.offset = 0.0         # калибровочное смещение
        self.calibrated = False
        self.sensitivity = 0.5    # градусов
        self.vertical_mode = False  # False - горизонталь, True - вертикаль
        self.beep_enabled = True
        self.simulating = False
        self.sim_speed = 0.1
        self.config_file = "level_config.json"
        self.load_config()
        
        # Canvas для визуализации уровня
        self.canvas = tk.Canvas(root, width=500, height=400, bg='lightgray')
        self.canvas.pack(pady=10)
        
        # Информационная метка
        self.info_label = tk.Label(root, text="Наклон: 0.0°", font=("Arial", 18))
        self.info_label.pack(pady=5)
        
        # Статус
        self.status = tk.Label(root, text="Готов", anchor=tk.W)
        self.status.pack(fill=tk.X, padx=10)
        
        # Панель управления
        control_frame = tk.Frame(root)
        control_frame.pack(pady=5)
        
        tk.Button(control_frame, text="Прочитать", command=self.read_angle).pack(side=tk.LEFT, padx=5)
        tk.Button(control_frame, text="Калибровка", command=self.calibrate).pack(side=tk.LEFT, padx=5)
        tk.Button(control_frame, text="Сброс", command=self.reset).pack(side=tk.LEFT, padx=5)
        tk.Button(control_frame, text="Симуляция", command=self.toggle_simulation).pack(side=tk.LEFT, padx=5)
        tk.Button(control_frame, text="Вертикаль", command=self.toggle_vertical).pack(side=tk.LEFT, padx=5)
        tk.Button(control_frame, text="Звук", command=self.toggle_beep).pack(side=tk.LEFT, padx=5)
        
        # Ползунок чувствительности
        sens_frame = tk.Frame(root)
        sens_frame.pack(pady=5)
        tk.Label(sens_frame, text="Чувствительность:").pack(side=tk.LEFT)
        self.sens_scale = tk.Scale(sens_frame, from_=0.1, to=5.0, resolution=0.1, orient=tk.HORIZONTAL, length=200)
        self.sens_scale.set(self.sensitivity)
        self.sens_scale.pack(side=tk.LEFT, padx=10)
        tk.Button(sens_frame, text="Применить", command=self.apply_sensitivity).pack(side=tk.LEFT)
        
        self.draw_level()
        self.update_loop()
        self.root.protocol("WM_DELETE_WINDOW", self.on_close)

    def draw_level(self):
        self.canvas.delete("all")
        w = 500
        h = 400
        cx, cy = w//2, h//2
        # Внешняя рамка
        self.canvas.create_rectangle(50, 50, w-50, h-50, outline='black', width=3)
        # Шкала
        for deg in range(-30, 31, 5):
            x = cx + deg * 5  # масштаб
            y_top = 80
            y_bottom = h-80
            self.canvas.create_line(x, y_top, x, y_bottom, fill='black')
            if deg % 10 == 0:
                self.canvas.create_text(x, y_top-10, text=str(deg), font=("Arial", 8))
        # Центральная метка
        self.canvas.create_line(cx, 60, cx, h-60, fill='red', width=2, dash=(5,3))
        # Пузырёк (будет двигаться)
        self.bubble = self.canvas.create_oval(cx-20, cy-20, cx+20, cy+20, fill='blue', outline='')
        self.update_bubble()

    def update_bubble(self):
        if not hasattr(self, 'bubble'):
            return
        w = 500
        h = 400
        cx, cy = w//2, h//2
        display_angle = (self.angle + self.offset) % 360
        # В режиме вертикали - угол 90° смещён
        if self.vertical_mode:
            # Для вертикали показываем отклонение от вертикали (90°)
            angle_deg = display_angle - 90
            # Нормализуем к диапазону -30..30
        else:
            angle_deg = display_angle
            if angle_deg > 180:
                angle_deg -= 360
        # Ограничиваем отображение -30..30 градусов
        if angle_deg > 30:
            angle_deg = 30
        elif angle_deg < -30:
            angle_deg = -30
        x = cx + angle_deg * 5
        self.canvas.coords(self.bubble, x-20, cy-20, x+20, cy+20)
        # Цвет пузырька в зависимости от отклонения
        if abs(angle_deg) < self.sensitivity:
            color = 'green'
        elif abs(angle_deg) < self.sensitivity*3:
            color = 'orange'
        else:
            color = 'red'
        self.canvas.itemconfig(self.bubble, fill=color)
        # Обновляем информацию
        display_angle_final = (self.angle + self.offset) % 360
        if self.vertical_mode:
            deviation = display_angle_final - 90
            if deviation > 180:
                deviation -= 360
            self.info_label.config(text=f"Отклонение от вертикали: {deviation:.1f}°")
            status_text = "Вертикально!" if abs(deviation) < self.sensitivity else f"Наклон: {deviation:.1f}°"
        else:
            deviation = display_angle_final
            if deviation > 180:
                deviation -= 360
            self.info_label.config(text=f"Наклон: {deviation:.1f}°")
            status_text = "Горизонтально!" if abs(deviation) < self.sensitivity else f"Наклон: {deviation:.1f}°"
        self.status.config(text=status_text)
        # Звуковой сигнал при достижении уровня
        if self.beep_enabled and abs(deviation) < self.sensitivity:
            if SOUND_AVAILABLE:
                winsound.Beep(1000, 100)
            else:
                print('\a', end='', flush=True)

    def update_loop(self):
        self.update_bubble()
        if self.simulating:
            self.angle += self.sim_speed
            if self.angle > 360:
                self.angle -= 360
        self.root.after(50, self.update_loop)

    def read_angle(self):
        # Имитация считывания с датчика
        if not self.simulating:
            self.angle += random.uniform(-1, 1)
            if self.angle > 360:
                self.angle -= 360
            elif self.angle < 0:
                self.angle += 360
        self.status.config(text="Измерение выполнено")

    def calibrate(self):
        # Калибровка: усреднение 10 измерений
        self.status.config(text="Калибровка... (имитация)")
        samples = []
        for _ in range(10):
            samples.append(random.uniform(-2, 2))
        avg = sum(samples) / len(samples)
        self.offset = -avg
        self.calibrated = True
        self.status.config(text=f"Калибровка завершена. Смещение: {self.offset:.2f}°")
        self.save_config()

    def reset(self):
        self.offset = 0.0
        self.calibrated = False
        self.angle = 0.0
        self.status.config(text="Сброшено")

    def toggle_simulation(self):
        self.simulating = not self.simulating
        self.status.config(text="Симуляция " + ("включена" if self.simulating else "выключена"))

    def toggle_vertical(self):
        self.vertical_mode = not self.vertical_mode
        self.status.config(text="Режим: " + ("вертикаль" if self.vertical_mode else "горизонталь"))

    def toggle_beep(self):
        self.beep_enabled = not self.beep_enabled
        self.status.config(text="Звук " + ("включён" if self.beep_enabled else "выключен"))

    def apply_sensitivity(self):
        self.sensitivity = self.sens_scale.get()
        self.status.config(text=f"Чувствительность: {self.sensitivity:.1f}°")

    def load_config(self):
        if os.path.exists(self.config_file):
            with open(self.config_file, 'r') as f:
                data = json.load(f)
                self.offset = data.get('offset', 0.0)
                self.calibrated = data.get('calibrated', False)
                self.sensitivity = data.get('sensitivity', 0.5)
                self.vertical_mode = data.get('vertical_mode', False)
                self.beep_enabled = data.get('beep_enabled', True)

    def save_config(self):
        data = {
            'offset': self.offset,
            'calibrated': self.calibrated,
            'sensitivity': self.sensitivity,
            'vertical_mode': self.vertical_mode,
            'beep_enabled': self.beep_enabled
        }
        with open(self.config_file, 'w') as f:
            json.dump(data, f)

    def on_close(self):
        self.save_config()
        self.root.destroy()

if __name__ == "__main__":
    root = tk.Tk()
    app = Level(root)
    root.mainloop()
