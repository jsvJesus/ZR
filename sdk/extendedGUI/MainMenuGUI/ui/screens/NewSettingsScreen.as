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
      
      protected var videoBase:MenuButton2;
      
      protected var videoAdditional:MenuButton2;
      
      protected var audio:MenuButton2;
      
      protected var mouse:MenuButton2;
      
      protected var gui:MenuButton2;
      
      protected var keybinds:MenuButton2;
      
      protected var back:MenuButton2;
      
      public function NewSettingsScreen(id:String, depth:uint = 0, use3D:Boolean = false)
      {
         super(id,depth,use3D);
      }
      
      override protected function unfreeze(... args) : void
      {
         Dummy.visible = false;
         this.label = BreadCrumbs.DIV + Locale.getById("extendedGUI.SettingsWindow.caption");
         super.unfreeze();
         Logger.LogToChannel(Logger.DEBUG,"SettingsScreen.unfreeze");
      }
      
      override protected function init(... args) : void
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
         this.videoLabel.font = Base.lightFontName;
         this.videoLabel.size = 18;
         this.videoBase = new MenuButton2(this.videoBox);
         this.videoBase.$ = "extendedGUI.Settings.video_main_settings";
         this.videoBase.height = 35;
         this.videoBase.addEventListener(MouseEvent.CLICK,this.onVideoBaseHandler);
         this.videoAdditional = new MenuButton2(this.videoBox);
         this.videoAdditional.$ = "extendedGUI.Settings.video_additional_settings";
         this.videoAdditional.height = 35;
         this.videoAdditional.addEventListener(MouseEvent.CLICK,this.onVideoAdditionalHandler);
         this.audio = new MenuButton2(this.box);
         this.audio.paddingTop = 10;
         this.audio.$ = "extendedGUI.Settings.audio";
         this.audio.height = 35;
         this.audio.addEventListener(MouseEvent.CLICK,this.onAudioHandler);
         this.mouse = new MenuButton2(this.box);
         this.mouse.$ = "extendedGUI.Settings.mouse";
         this.mouse.height = 35;
         this.mouse.addEventListener(MouseEvent.CLICK,this.onMouseHandler);
         this.gui = new MenuButton2(this.box);
         this.gui.$ = "extendedGUI.Settings.gui";
         this.gui.height = 35;
         this.gui.addEventListener(MouseEvent.CLICK,this.onGUIHandler);
         this.keybinds = new MenuButton2(this.box);
         this.keybinds.$ = "extendedGUI.Settings.keybinds";
         this.keybinds.height = 35;
         this.keybinds.addEventListener(MouseEvent.CLICK,this.onKeybindsHandler);
         this.back = new MenuButton2(this.box);
         this.back.paddingTop = 10;
         this.back.$ = "extendedGUI.SettingsWindow.backButton";
         this.back.height = 35;
         this.back.addEventListener(MouseEvent.CLICK,this.onBackHandler);
      }
      
      protected function onVideoBaseHandler(event:MouseEvent) : void
      {
         Settings.Tune = new SettingsObject(["video","main_settings"]);
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.SETTINGS_TUNE_VIDEO_MAIN));
      }
      
      protected function onVideoAdditionalHandler(event:MouseEvent) : void
      {
         Settings.Tune = new SettingsObject(["video","additional_settings"]);
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.SETTINGS_TUNE));
      }
      
      protected function onAudioHandler(event:MouseEvent) : void
      {
         Settings.Tune = new SettingsObject(["audio"]);
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.SETTINGS_TUNE));
      }
      
      protected function onMouseHandler(event:MouseEvent) : void
      {
         Settings.Tune = new SettingsObject(["mouse"]);
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.SETTINGS_TUNE));
      }
      
      protected function onKeybindsHandler(event:MouseEvent) : void
      {
         Settings.Tune = new SettingsObject(["keybinds"]);
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.KEYBINDS_TUNE));
      }
      
      protected function onGUIHandler(event:MouseEvent) : void
      {
         Settings.Tune = new SettingsObject(["gui"]);
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.SETTINGS_TUNE));
      }
      
      protected function onBackHandler(event:MouseEvent) : void
      {
         this.goBack();
      }
      
      override protected function onKeyDown(event:KeyboardEvent) : void
      {
         super.onKeyDown(event);
         if(Base.stage.focus != this)
         {
            return;
         }
      }
      
      override public function goBack() : void
      {
         var tempError:Error = new Error();
         var stackTrace:String = tempError.getStackTrace();
         if(!this.is_loaded)
         {
            return;
         }
         if(Base.IN_GAME)
         {
            Api.call(Api.CLOSE_SETTINGS);
            return;
         }
         super.goBack();
         if(Base.navigator.is_auth)
         {
            this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.ROOT_SCREEN));
         }
         else
         {
            this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.LOGIN_SCREEN));
         }
      }
   }
}

