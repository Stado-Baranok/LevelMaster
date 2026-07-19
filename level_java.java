// level_java.java — строительный уровень с калибровкой на Java (Swing)

import javax.swing.*;
import java.awt.*;
import java.awt.event.*;
import java.awt.geom.*;
import java.util.Random;
import java.io.*;
import java.nio.file.*;

public class LevelJava extends JFrame {
    private static final String CONFIG_FILE = "level_config.json";
    private double angle = 0.0;
    private double offset = 0.0;
    private double sensitivity = 0.5;
    private boolean calibrated = false;
    private boolean simulating = false;
    private boolean verticalMode = false;
    private boolean beepEnabled = true;
    private Timer timer;
    private LevelPanel levelPanel;
    private JLabel infoLabel, statusLabel;
    private JSlider sensSlider;
    private Random rand = new Random();

    public LevelJava() {
        setTitle("📏 LevelMaster — Java");
        setSize(600, 550);
        setDefaultCloseOperation(EXIT_ON_CLOSE);
        setLayout(new BorderLayout());

        loadConfig();

        levelPanel = new LevelPanel();
        levelPanel.setSensitivity(sensitivity);
        add(levelPanel, BorderLayout.CENTER);

        JPanel controlPanel = new JPanel();
        JButton readBtn = new JButton("Прочитать");
        JButton calibrateBtn = new JButton("Калибровка");
        JButton resetBtn = new JButton("Сброс");
        JButton simBtn = new JButton("Симуляция");
        JButton vertBtn = new JButton("Вертикаль");
        JButton beepBtn = new JButton("Звук");
        controlPanel.add(readBtn);
        controlPanel.add(calibrateBtn);
        controlPanel.add(resetBtn);
        controlPanel.add(simBtn);
        controlPanel.add(vertBtn);
        controlPanel.add(beepBtn);
        add(controlPanel, BorderLayout.NORTH);

        infoLabel = new JLabel("Наклон: 0.0°", SwingConstants.CENTER);
        infoLabel.setFont(new Font("Arial", Font.PLAIN, 16));
        add(infoLabel, BorderLayout.CENTER);

        statusLabel = new JLabel("Готов", SwingConstants.CENTER);
        add(statusLabel, BorderLayout.SOUTH);

        // Ползунок чувствительности
        JPanel sensPanel = new JPanel(new FlowLayout());
        sensPanel.add(new JLabel("Чувствительность:"));
        sensSlider = new JSlider(1, 50, (int)(sensitivity*10));
        sensSlider.addChangeListener(e -> {
            sensitivity = sensSlider.getValue() / 10.0;
            levelPanel.setSensitivity(sensitivity);
            statusLabel.setText("Чувствительность: " + sensitivity + "°");
        });
        sensPanel.add(sensSlider);
        add(sensPanel, BorderLayout.SOUTH);

        readBtn.addActionListener(e -> readAngle());
        calibrateBtn.addActionListener(e -> calibrate());
        resetBtn.addActionListener(e -> reset());
        simBtn.addActionListener(e -> toggleSimulation());
        vertBtn.addActionListener(e -> toggleVertical());
        beepBtn.addActionListener(e -> toggleBeep());

        timer = new Timer(50, e -> simulate());
        updateDisplay();
    }

    private void readAngle() {
        if (!simulating) {
            angle += rand.nextDouble() * 2 - 1;
            if (angle > 360) angle -= 360;
            if (angle < 0) angle += 360;
        }
        updateDisplay();
    }

    private void calibrate() {
        statusLabel.setText("Калибровка... (имитация)");
        double sum = 0;
        for (int i = 0; i < 10; i++) {
            sum += rand.nextDouble() * 4 - 2;
        }
        double avg = sum / 10;
        offset = -avg;
        calibrated = true;
        statusLabel.setText(String.format("Калибровка завершена. Смещение: %.2f°", offset));
        saveConfig();
        updateDisplay();
    }

    private void reset() {
        offset = 0.0;
        calibrated = false;
        angle = 0.0;
        statusLabel.setText("Сброшено");
        updateDisplay();
    }

    private void toggleSimulation() {
        simulating = !simulating;
        if (simulating) {
            timer.start();
            statusLabel.setText("Симуляция включена");
        } else {
            timer.stop();
            statusLabel.setText("Симуляция выключена");
        }
    }

    private void simulate() {
        angle = (angle + 0.2) % 360;
        updateDisplay();
    }

    private void toggleVertical() {
        verticalMode = !verticalMode;
        statusLabel.setText("Режим: " + (verticalMode ? "вертикаль" : "горизонталь"));
        updateDisplay();
    }

    private void toggleBeep() {
        beepEnabled = !beepEnabled;
        statusLabel.setText("Звук " + (beepEnabled ? "включён" : "выключен"));
    }

    private void updateDisplay() {
        double displayAngle = angle + offset;
        if (displayAngle > 360) displayAngle -= 360;
        if (displayAngle < 0) displayAngle += 360;
        levelPanel.setAngle(displayAngle);
        double deviation = displayAngle;
        if (verticalMode) deviation -= 90;
        if (deviation > 180) deviation -= 360;
        if (verticalMode)
            infoLabel.setText(String.format("Отклонение от вертикали: %.1f°", deviation));
        else
            infoLabel.setText(String.format("Наклон: %.1f°", deviation));
        if (beepEnabled && Math.abs(deviation) < sensitivity) {
            Toolkit.getDefaultToolkit().beep();
        }
        levelPanel.repaint();
    }

    private void loadConfig() {
        try {
            String content = new String(Files.readAllBytes(Paths.get(CONFIG_FILE)));
            // Упрощённый парсинг
            if (content.contains("offset")) {
                // Пропускаем для простоты
            }
        } catch (IOException e) {}
    }

    private void saveConfig() {
        try (PrintWriter pw = new PrintWriter(CONFIG_FILE)) {
            pw.println("{\"offset\":" + offset + ",\"calibrated\":" + calibrated + ",\"sensitivity\":" + sensitivity + ",\"verticalMode\":" + verticalMode + ",\"beepEnabled\":" + beepEnabled + "}");
        } catch (IOException e) {}
    }

    class LevelPanel extends JPanel {
        private double angle = 0.0;
        private double sensitivity = 0.5;

        public void setAngle(double deg) { angle = deg; }
        public void setSensitivity(double sens) { sensitivity = sens; }

        @Override
        protected void paintComponent(Graphics g) {
            super.paintComponent(g);
            Graphics2D g2 = (Graphics2D) g;
            g2.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON);
            int w = getWidth();
            int h = getHeight();
            int cx = w/2, cy = h/2;
            // Рамка
            g2.drawRect(50, 50, w-100, h-100);
            // Шкала
            for (int deg = -30; deg <= 30; deg += 5) {
                int x = cx + deg * 5;
                g2.drawLine(x, 80, x, h-80);
                if (deg % 10 == 0) {
                    g2.drawString(String.valueOf(deg), x-10, 70);
                }
            }
            // Центр
            g2.setColor(Color.RED);
            g2.setStroke(new BasicStroke(2, BasicStroke.CAP_BUTT, BasicStroke.JOIN_BEVEL, 0, new float[]{5,5}, 0));
            g2.drawLine(cx, 60, cx, h-60);
            g2.setStroke(new BasicStroke(1));
            // Пузырёк
            double displayAngle = angle;
            if (displayAngle > 180) displayAngle -= 360;
            if (displayAngle > 30) displayAngle = 30;
            if (displayAngle < -30) displayAngle = -30;
            int x = cx + (int)(displayAngle * 5);
            Color color;
            if (Math.abs(displayAngle) < sensitivity) color = Color.GREEN;
            else if (Math.abs(displayAngle) < sensitivity*3) color = Color.ORANGE;
            else color = Color.RED;
            g2.setColor(color);
            g2.fillOval(x-20, cy-20, 40, 40);
        }
    }

    public static void main(String[] args) {
        SwingUtilities.invokeLater(() -> {
            try {
                UIManager.setLookAndFeel(UIManager.getSystemLookAndFeelClassName());
            } catch (Exception e) {}
            new LevelJava().setVisible(true);
        });
    }
}
