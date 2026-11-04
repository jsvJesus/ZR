package ui.screens
{
   import com.dvalimona.components.*;
   import communication.*;
   import events.*;
   import flash.events.*;
   import lang.*;
   import logging.*;
   import ui.Screen;
   
   public class NewSettingsScreen extends Screen
   {
      protected var box:VBox;
      
      protected var videoBox:VBox;
      
      protected var videoLabel:LabelShadowed;
      
      protected var videoBase:MenuButton;
      
      protected var videoAdditional:MenuButton;
      
      protected var audio:MenuButton;
      
      protected var mouse:MenuButton;
      
      protected var keybinds:MenuButton;
      
      protected var back:MenuButton;
      
      public function NewSettingsScreen(param1:String, param2:uint = 0, param3:Boolean = false)
      {
         super(param1,param2,param3);
      }
      
      override protected function unfreeze(... rest) : void
      {
         Dummy.visible = false;
         this.label = BreadCrumbs.DIV + Locale.getById("extendedGUI.SettingsWindow.caption");
         super.unfreeze();
         Logger.LogToChannel(Logger.DEBUG,"SettingsScreen.unfreeze");
      }
      
      override protected function init(... rest) : void
      {
         this.box = new VBox(this);
         this.box.x = 40;
         this.box.y = 100;
         this.box.alignment = VBox.LEFT;
         this.box.spacing = 1;
         this.box.debug = false;
         this.box.debugAlpha = 0.1;
         this.box.debugColor = 255;
         this.videoBox = new VBox(this.box);
         this.videoBox.alignment = VBox.LEFT;
         this.videoBox.spacing = 1;
         this.videoBox.paddingLeft = -10;
         this.videoBox.backgroundColor = 0;
         this.videoBox.backgroundAlpha = 0.5;
         this.videoBox.marginTop = 10;
         this.videoBox.marginBottom = 10;
         this.videoBox.marginLeft = 10;
         this.videoBox.marginRight = 10;
         this.videoLabel = new LabelShadowed(this.videoBox);
         this.videoLabel.$ = "extendedGUI.Settings.video";
         this.videoLabel.color = 12895428;
         this.videoLabel.font = Base.FONT_LIGHT;
         this.videoLabel.size = 18;
         this.videoBase = new MenuButton(this.videoBox);
         this.videoBase.$ = "extendedGUI.Settings.video_main_settings";
         this.videoBase.height = 35;
         this.videoBase.addEventListener(MouseEvent.CLICK,this.onVideoBaseHandler);
         this.videoAdditional = new MenuButton(this.videoBox);
         this.videoAdditional.$ = "extendedGUI.Settings.video_additional_settings";
         this.videoAdditional.height = 35;
         this.videoAdditional.addEventListener(MouseEvent.CLICK,this.onVideoAdditionalHandler);
         this.audio = new MenuButton(this.box);
         this.audio.paddingTop = 10;
         this.audio.$ = "extendedGUI.Settings.audio";
         this.audio.height = 35;
         this.audio.addEventListener(MouseEvent.CLICK,this.onAudioHandler);
         this.mouse = new MenuButton(this.box);
         this.mouse.$ = "extendedGUI.Settings.mouse";
         this.mouse.height = 35;
         this.mouse.addEventListener(MouseEvent.CLICK,this.onMouseHandler);
         this.keybinds = new MenuButton(this.box);
         this.keybinds.$ = "extendedGUI.Settings.keybinds";
         this.keybinds.height = 35;
         this.keybinds.addEventListener(MouseEvent.CLICK,this.onKeybindsHandler);
         this.back = new MenuButton(this.box);
         this.back.paddingTop = 10;
         this.back.$ = "extendedGUI.SettingsWindow.backButton";
         this.back.height = 35;
         this.back.addEventListener(MouseEvent.CLICK,this.onBackHandler);
      }
      
      protected function onVideoBaseHandler(param1:MouseEvent) : void
      {
         Settings.Tune = new SettingsObject(["video","main_settings"]);
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.SETTINGS_TUNE_VIDEO_MAIN));
      }
      
      protected function onVideoAdditionalHandler(param1:MouseEvent) : void
      {
         Settings.Tune = new SettingsObject(["video","additional_settings"]);
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.SETTINGS_TUNE));
      }
      
      protected function onAudioHandler(param1:MouseEvent) : void
      {
         Settings.Tune = new SettingsObject(["audio"]);
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.SETTINGS_TUNE));
      }
      
      protected function onMouseHandler(param1:MouseEvent) : void
      {
         Settings.Tune = new SettingsObject(["mouse"]);
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.SETTINGS_TUNE));
      }
      
      protected function onKeybindsHandler(param1:MouseEvent) : void
      {
         Settings.Tune = new SettingsObject(["keybinds"]);
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.KEYBINDS_TUNE));
      }
      
      protected function onBackHandler(param1:MouseEvent) : void
      {
         this.goBack();
      }
      
      override public function goBack() : void
      {
         super.goBack();
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.ROOT_SCREEN));
      }
   }
}

