// level_cpp.cpp — строительный уровень с калибровкой на C++ (Qt)

#include <QApplication>
#include <QMainWindow>
#include <QWidget>
#include <QVBoxLayout>
#include <QHBoxLayout>
#include <QPushButton>
#include <QLabel>
#include <QGraphicsView>
#include <QGraphicsScene>
#include <QGraphicsEllipseItem>
#include <QGraphicsLineItem>
#include <QTimer>
#include <QSettings>
#include <QMessageBox>
#include <QInputDialog>
#include <QSound>
#include <random>

class LevelWidget : public QGraphicsView {
    Q_OBJECT
public:
    LevelWidget(QWidget *parent = nullptr) : QGraphicsView(parent) {
        scene = new QGraphicsScene(this);
        setScene(scene);
        setRenderHint(QPainter::Antialiasing);
        setFixedSize(500, 400);
        drawLevel();
    }

    void setAngle(double deg) {
        angle = deg;
        updateBubble();
    }

private:
    QGraphicsScene *scene;
    QGraphicsEllipseItem *bubble;
    double angle = 0.0;
    double sensitivity = 0.5;
    bool verticalMode = false;

    void drawLevel() {
        scene->clear();
        int w = 500, h = 400;
        int cx = w/2, cy = h/2;
        // Рамка
        scene->addRect(50, 50, w-100, h-100, QPen(Qt::black, 3));
        // Шкала
        for (int deg = -30; deg <= 30; deg += 5) {
            int x = cx + deg * 5;
            scene->addLine(x, 80, x, h-80, QPen(Qt::black));
            if (deg % 10 == 0) {
                auto text = scene->addText(QString::number(deg));
                text->setPos(x-10, 70);
            }
        }
        // Центр
        scene->addLine(cx, 60, cx, h-60, QPen(Qt::red, 2, Qt::DashLine));
        // Пузырёк
        bubble = scene->addEllipse(cx-20, cy-20, 40, 40, QPen(Qt::NoPen), QBrush(Qt::blue));
        updateBubble();
    }

    void updateBubble() {
        if (!bubble) return;
        int w = 500, h = 400;
        int cx = w/2, cy = h/2;
        double displayAngle = angle; // уже с учётом смещения
        if (verticalMode) {
            displayAngle -= 90;
        }
        // Нормализация
        if (displayAngle > 180) displayAngle -= 360;
        // Ограничение
        if (displayAngle > 30) displayAngle = 30;
        if (displayAngle < -30) displayAngle = -30;
        int x = cx + displayAngle * 5;
        bubble->setRect(x-20, cy-20, 40, 40);
        // Цвет
        QColor color;
        if (abs(displayAngle) < sensitivity) color = Qt::green;
        else if (abs(displayAngle) < sensitivity*3) color = Qt::yellow;
        else color = Qt::red;
        bubble->setBrush(QBrush(color));
    }
};

class MainWindow : public QMainWindow {
    Q_OBJECT
public:
    MainWindow(QWidget *parent = nullptr) : QMainWindow(parent) {
        setWindowTitle("📏 LevelMaster — C++");
        QWidget *central = new QWidget(this);
        setCentralWidget(central);
        QVBoxLayout *mainLayout = new QVBoxLayout(central);

        levelWidget = new LevelWidget(this);
        mainLayout->addWidget(levelWidget);

        infoLabel = new QLabel("Наклон: 0.0°");
        infoLabel->setAlignment(Qt::AlignCenter);
        infoLabel->setStyleSheet("font-size: 16px;");
        mainLayout->addWidget(infoLabel);

        statusLabel = new QLabel("Готов");
        mainLayout->addWidget(statusLabel);

        QHBoxLayout *btnLayout = new QHBoxLayout();
        QPushButton *readBtn = new QPushButton("Прочитать");
        QPushButton *calibrateBtn = new QPushButton("Калибровка");
        QPushButton *resetBtn = new QPushButton("Сброс");
        QPushButton *simBtn = new QPushButton("Симуляция");
        QPushButton *vertBtn = new QPushButton("Вертикаль");
        QPushButton *beepBtn = new QPushButton("Звук");
        btnLayout->addWidget(readBtn);
        btnLayout->addWidget(calibrateBtn);
        btnLayout->addWidget(resetBtn);
        btnLayout->addWidget(simBtn);
        btnLayout->addWidget(vertBtn);
        btnLayout->addWidget(beepBtn);
        mainLayout->addLayout(btnLayout);

        // Чувствительность
        QHBoxLayout *sensLayout = new QHBoxLayout();
        sensLayout->addWidget(new QLabel("Чувствительность:"));
        sensSpin = new QDoubleSpinBox;
        sensSpin->setRange(0.1, 5.0);
        sensSpin->setSingleStep(0.1);
        sensSpin->setValue(sensitivity);
        connect(sensSpin, QOverload<double>::of(&QDoubleSpinBox::valueChanged), this, &MainWindow::applySensitivity);
        sensLayout->addWidget(sensSpin);
        mainLayout->addLayout(sensLayout);

        timer = new QTimer(this);
        connect(timer, &QTimer::timeout, this, &MainWindow::simulate);

        connect(readBtn, &QPushButton::clicked, this, &MainWindow::readAngle);
        connect(calibrateBtn, &QPushButton::clicked, this, &MainWindow::calibrate);
        connect(resetBtn, &QPushButton::clicked, this, &MainWindow::reset);
        connect(simBtn, &QPushButton::clicked, this, &MainWindow::toggleSimulation);
        connect(vertBtn, &QPushButton::clicked, this, &MainWindow::toggleVertical);
        connect(beepBtn, &QPushButton::clicked, this, &MainWindow::toggleBeep);

        loadConfig();
        updateDisplay();
    }

private slots:
    void readAngle() {
        if (!simulating) {
            static std::random_device rd;
            static std::mt19937 gen(rd());
            static std::uniform_real_distribution<> dis(-1, 1);
            angle += dis(gen);
            if (angle > 360) angle -= 360;
            if (angle < 0) angle += 360;
        }
        updateDisplay();
    }

    void calibrate() {
        statusLabel->setText("Калибровка... (имитация)");
        double sum = 0;
        for (int i = 0; i < 10; ++i) {
            sum += (rand() % 100) / 50.0 - 1.0;
        }
        double avg = sum / 10.0;
        offset = -avg;
        calibrated = true;
        statusLabel->setText(QString("Калибровка завершена. Смещение: %1°").arg(offset, 0, 'f', 2));
        saveConfig();
        updateDisplay();
    }

    void reset() {
        offset = 0.0;
        calibrated = false;
        angle = 0.0;
        statusLabel->setText("Сброшено");
        updateDisplay();
    }

    void toggleSimulation() {
        simulating = !simulating;
        if (simulating) {
            timer->start(50);
            statusLabel->setText("Симуляция включена");
        } else {
            timer->stop();
            statusLabel->setText("Симуляция выключена");
        }
    }

    void toggleVertical() {
        verticalMode = !verticalMode;
        statusLabel->setText("Режим: " + QString(verticalMode ? "вертикаль" : "горизонталь"));
        updateDisplay();
    }

    void toggleBeep() {
        beepEnabled = !beepEnabled;
        statusLabel->setText("Звук " + QString(beepEnabled ? "включён" : "выключен"));
    }

    void applySensitivity(double val) {
        sensitivity = val;
        levelWidget->setAngle((angle + offset));
        statusLabel->setText(QString("Чувствительность: %1°").arg(val));
    }

    void simulate() {
        angle += 0.2;
        if (angle > 360) angle -= 360;
        updateDisplay();
    }

private:
    LevelWidget *levelWidget;
    QLabel *infoLabel, *statusLabel;
    QDoubleSpinBox *sensSpin;
    QTimer *timer;
    double angle = 0.0;
    double offset = 0.0;
    double sensitivity = 0.5;
    bool calibrated = false;
    bool simulating = false;
    bool verticalMode = false;
    bool beepEnabled = true;

    void updateDisplay() {
        double displayAngle = angle + offset;
        if (displayAngle > 360) displayAngle -= 360;
        if (displayAngle < 0) displayAngle += 360;
        levelWidget->setAngle(displayAngle);
        double deviation = displayAngle;
        if (verticalMode) deviation -= 90;
        if (deviation > 180) deviation -= 360;
        QString text = verticalMode ? QString("Отклонение от вертикали: %1°").arg(deviation, 0, 'f', 1) :
                                    QString("Наклон: %1°").arg(deviation, 0, 'f', 1);
        infoLabel->setText(text);
        if (beepEnabled && qAbs(deviation) < sensitivity) {
            QSound::beep();
        }
    }

    void loadConfig() {
        QSettings settings("MyApp", "LevelMaster");
        offset = settings.value("offset", 0.0).toDouble();
        calibrated = settings.value("calibrated", false).toBool();
        sensitivity = settings.value("sensitivity", 0.5).toDouble();
        verticalMode = settings.value("verticalMode", false).toBool();
        beepEnabled = settings.value("beepEnabled", true).toBool();
        sensSpin->setValue(sensitivity);
    }

    void saveConfig() {
        QSettings settings("MyApp", "LevelMaster");
        settings.setValue("offset", offset);
        settings.setValue("calibrated", calibrated);
        settings.setValue("sensitivity", sensitivity);
        settings.setValue("verticalMode", verticalMode);
        settings.setValue("beepEnabled", beepEnabled);
    }
};

int main(int argc, char *argv[]) {
    QApplication app(argc, argv);
    MainWindow w;
    w.show();
    return app.exec();
}

#include "level_cpp.moc"
