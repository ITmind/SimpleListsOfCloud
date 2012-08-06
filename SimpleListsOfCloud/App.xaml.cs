using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Live;
using Mogade.WindowsPhone;

namespace SimpleListsOfCloud
{
    public partial class App : Application
    {
        /// <summary>
        /// Обеспечивает быстрый доступ к корневому кадру приложения телефона.
        /// </summary>
        /// <returns>Корневой кадр приложения телефона.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }
        public LiveConnectSession LiveSession { get; set; }
        public ItemsList ListItems { get; set; }
        public SkyDriveFolders SkyDriveFolders { get; set; }
        public IMogadeClient Mogade { get; private set; }
        SettingsData settingsData;

        public SettingsData Settings
        {
            get { return settingsData; }
            set
            {
                settingsData = value;
            }
        }

        public static new App Current
        {
            get { return Application.Current as App; }
        }
        /// <summary>
        /// Конструктор объекта приложения.
        /// </summary>
        public App()
        {
            // Глобальный обработчик неперехваченных исключений. 
            UnhandledException += Application_UnhandledException;

            // Стандартная инициализация Silverlight
            InitializeComponent();

            // Инициализация телефона
            InitializePhoneApplication();

            // Отображение сведений о профиле графики во время отладки.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Отображение текущих счетчиков частоты смены кадров.
                Application.Current.Host.Settings.EnableFrameRateCounter = false;

                // Отображение областей приложения, перерисовываемых в каждом кадре.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Включение режима визуализации анализа нерабочего кода 
                // для отображения областей страницы, переданных в GPU, с цветным наложением.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Отключите обнаружение простоя приложения, установив для свойства UserIdleDetectionMode
                // объекта PhoneApplicationService приложения значение Disabled.
                // Внимание! Используйте только в режиме отладки. Приложение, в котором отключено обнаружение бездействия пользователя, будет продолжать работать
                // и потреблять энергию батареи, когда телефон не будет использоваться.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

            ListItems = new ItemsList();
            SkyDriveFolders = new SkyDriveFolders();

        }

        // Код для выполнения при запуске приложения (например, из меню "Пуск")
        // Этот код не будет выполняться при повторной активации приложения
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            ListItems.Load();
            IsolatedStorageSettings iss = IsolatedStorageSettings.ApplicationSettings;
            if (!iss.TryGetValue("Settings", out settingsData))
            {
                Settings = new SettingsData();
            }


            Mogade = MogadeHelper.CreateInstance();
#if !DEBUG
            Mogade.LogApplicationStart();
#endif
        }

        // Код для выполнения при активации приложения (переводится в основной режим)
        // Этот код не будет выполняться при первом запуске приложения
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            ListItems.Load();
            if (e.IsApplicationInstancePreserved)
            {
                return;
            }

            IsolatedStorageSettings iss = IsolatedStorageSettings.ApplicationSettings;
            if (PhoneApplicationService.Current.State.ContainsKey("Settings"))
            {
                Settings = PhoneApplicationService.Current.State["Settings"] as SettingsData;
            }
            else
            {
                if (!iss.TryGetValue("Settings", out settingsData))
                {
                    Settings = new SettingsData();
                }
            }

            Mogade = MogadeHelper.CreateInstance();
#if !DEBUG
            Mogade.LogApplicationStart();
#endif
        }

        // Код для выполнения при деактивации приложения (отправляется в фоновый режим)
        // Этот код не будет выполняться при закрытии приложения
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            ListItems.Save();
            PhoneApplicationService.Current.State["Settings"] = Settings;
            SaveStateToIsolatedStorage();
        }

        // Код для выполнения при закрытии приложения (например, при нажатии пользователем кнопки "Назад")
        // Этот код не будет выполняться при деактивации приложения
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            ListItems.Save();
            SaveStateToIsolatedStorage();
        }

        public void SaveStateToIsolatedStorage()
        {
            IsolatedStorageSettings iss = IsolatedStorageSettings.ApplicationSettings;
            if (!iss.Contains("Settings"))
            {
                iss.Add("Settings", Settings);
            }
            else
            {
                iss["Settings"] = Settings;
            }

        }
        // Код для выполнения в случае ошибки навигации
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Ошибка навигации; перейти в отладчик
                System.Diagnostics.Debugger.Break();
            }
        }

        // Код для выполнения на необработанных исключениях
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Произошло необработанное исключение; перейти в отладчик
                System.Diagnostics.Debugger.Break();
            }
        }

        #region Инициализация приложения телефона

        // Избегайте двойной инициализации
        private bool phoneApplicationInitialized = false;

        // Не добавляйте в этот метод дополнительный код
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Создайте кадр, но не задавайте для него значение RootVisual; это позволит
            // экрану-заставке оставаться активным, пока приложение не будет готово для визуализации.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Обработка сбоев навигации
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Убедитесь, что инициализация не выполняется повторно
            phoneApplicationInitialized = true;
        }

        // Не добавляйте в этот метод дополнительный код
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Задайте корневой визуальный элемент для визуализации приложения
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Удалите этот обработчик, т.к. он больше не нужен
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion
    }
}