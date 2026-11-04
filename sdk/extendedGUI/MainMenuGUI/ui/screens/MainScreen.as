package ui.screens
{
   import com.dvalimona.components.*;
   import communication.*;
   import events.*;
   import flash.display.*;
   import flash.events.*;
   import flash.text.*;
   import flash.ui.*;
   import flash.utils.*;
   import lang.*;
   import logging.*;
   import ui.*;
   import ui.components.*;
   
   public class MainScreen extends Screen
   {
      private var offsetChar:int = 488;
      
      private var offsetStart:int = 424;
      
      private var vBox:VBox;
      
      private var charBox:VBox;
      
      private var rightBox:VBox;
      
      private var COUNT_CHARS:uint = 0;
      
      private var charList:Array = new Array();
      
      private var premium:NewPremiumPanel;
      
      private var modItems:NewPanelWithIcon;
      
      private var modFace:NewPanelWithIcon;
      
      private var eventsPanel:EventsPanel;
      
      private var surveyPanel:SurveyPanel;
      
      private var settings:MenuButton2;
      
      private var promo:MenuButton2;
      
      private var cutscene:MenuButton2;
      
      private var partner:MenuButton2;
      
      private var support:MenuButton2;
      
      private var myChars:MenuButton2;
      
      private var news:MenuButton2;
      
      private var licenseBtn:MenuButton2;
      
      private var quit:MenuButton2;
      
      private var toGame:MenuButton2;
      
      private var toGameBackGround:Bitmap;
      
      private var __crutchTextField:TextField;
      
      private var __tempSprite:Sprite;
      
      private var stupid_flag_for_DAUN_ANTON:Boolean = false;
      
      public function MainScreen(id:String, depth:uint = 0, use3D:Boolean = false)
      {
         Auth.self.addEventListener(Auth.AUTH_SUCCESS,this.onAuthSuccess);
         Character.core.addEventListener(Character.CHANGE,this.onCharacterChange);
         Character.core.addEventListener(Character.UPDATED,this.onCharactersUpdated);
         Api.self.addEventListener(Api.CHANGE_FACE_BTN_ENABLED,this.onChangeEditBtnEnabled);
         Api.self.addEventListener(Api.PARTNER_ID_CHANGE,this.onChangePartnerID);
         Api.self.addEventListener(Api.CALL_SURVEY,this.showsurvey);
         Api.self.addEventListener(Api.HIDE_SURVEY,this.hidesurvey);
         Api.self.addEventListener(Api.ALL_CHARACTERS_INFO,this.onAllCharInfoHandler);
         super(id,depth,use3D);
      }
      
      protected function onAuthSuccess(event:Event) : void
      {
      }
      
      override protected function freeze(... args) : void
      {
      }
      
      override protected function unfreeze(... args) : void
      {
         setTimeout(this.unfreezeDeals,0);
      }
      
      private function unfreezeDeals() : void
      {
         this.label = Locale.getById("extendedGUI.RootWindow.caption");
         Character.Update();
         setTimeout(Base.navigator.header.invalidateGold,500);
         setTimeout(Base.navigator.header.invalidateGold,1000);
         super.unfreeze();
         setTimeout(this.updateToGameButton,100);
         setTimeout(this.updateToGameButton,500);
         setTimeout(this.updateToGameButton,1500);
         this.checkEULA();
         Dummy.visible = true;
         this.premium.updateView();
      }
      
      private function alignStartButton() : void
      {
      }
      
      private function checkEULA() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"EULA.Status",EULA.Status);
         if(EULA.Status == -1)
         {
            Logger.LogToChannel(Logger.DEBUG,"EULA.Status == undefined");
            EULA.self.addEventListener(EULA.STATUS_EVENT,this.onEulaHandler);
            EULA.requestStatus();
         }
         else
         {
            this.EULADeals();
         }
      }
      
      private function EULADeals() : void
      {
         var eulaText:* = null;
         if(EULA.Status == 1)
         {
            Logger.LogToChannel(Logger.DEBUG,"EULA.Status == 1");
         }
         else if(EULA.Status == 0)
         {
            Logger.LogToChannel(Logger.DEBUG,"EULA.Status == 0");
            eulaText = Locale.getById("EULA.eula_text.topic") + "\n\n" + Locale.getById("EULA.eula_text.paragraph") + "\n\n" + Locale.getById("EULA.eula_text.resume") + "\n\n";
            Base.navigator.showDialog("EULA ",eulaText,true,[new DialogButtonItem("EULA.buttons.accept",this.setEulaAccepted,0.4,[Keyboard.ENTER]),new DialogButtonItem("EULA.buttons.cancel",this.setEulaDeclined,0.6,[Keyboard.ESCAPE])],800,500);
         }
      }
      
      private function setEulaAccepted() : void
      {
         Api.call(Api.SET_EULA_ACCEPTED,[{"eula":1}]);
         EULA.Status = 1;
      }
      
      private function setEulaDeclined() : void
      {
         Api.call(Api.QUIT_GAME);
      }
      
      protected function onEulaHandler(event:Event) : void
      {
         EULA.self.removeEventListener(EULA.STATUS_EVENT,this.onEulaHandler);
         this.EULADeals();
      }
      
      override protected function init(... args) : void
      {
         var charBtn:* = undefined;
         this.vBox = new VBox(this);
         this.vBox.alignment = VBox.LEFT;
         this.vBox.spacing = 1;
         this.vBox.fixedWidth = 413;
         this.vBox.width = 413;
         this.vBox.x = 35;
         this.vBox.y = 93;
         this.toGame = new MenuButton2(this.vBox);
         this.toGame.$ = "extendedGUI.RootWindow.startGameButton";
         this.toGame.size = 30;
         this.toGame.is_send_sound = false;
         this.toGame.shadowAlpha = 0;
         this.toGame.autoWidth = false;
         this.toGame.height = 50;
         this.toGame.align = Label.CENTER;
         this.toGame.width = 195;
         this.toGame.paddingLeft = 0;
         this.toGame.paddingBottom = 0;
         this.toGame.font = Base.boldFontName;
         this.toGame.labelOverColor = this.toGame.labelUpColor;
         this.toGame.upColorAlpha = 1;
         this.toGame.upColor = 6298395;
         this.toGame.overColor = 10830629;
         this.toGame.downColor = this.toGame.upColor;
         this.toGame.downColorAlpha = 0.9;
         this.toGame.overColorAlpha = 1;
         this.toGame.addEventListener(MouseEvent.CLICK,this.onToGameClick);
         this.charBox = new VBox(this.vBox);
         this.charBox._debug = true;
         this.charBox.alignment = VBox.LEFT;
         this.charBox.spacing = 1;
         this.charBox.width = 413;
         this.charBox.fixedWidth = 413;
         this.charBox.backgroundAlpha = 0.5;
         this.charBox.backgroundColor = 0;
         this.charBox.paddingTop = 0;
         this.charBox.invalidate();
         this.myChars = new MenuButton2(this.charBox);
         this.myChars.$ = "extendedGUI.CharWindow.yourChars";
         this.myChars.autoWidth = false;
         this.myChars.height = 35;
         this.myChars.width = 413;
         this.myChars.paddingTop = 8;
         this.myChars.paddingBottom = 5;
         this.myChars.paddingLeft += 10;
         this.myChars.labelUpColor = 7500402;
         this.myChars.mouseEnabled = false;
         this.rightBox = new VBox(this);
         this.rightBox.alignment = VBox.RIGHT;
         this.rightBox.spacing = 1;
         this.rightBox.width = 500;
         this.__crutchTextField = new TextField();
         this.__crutchTextField.autoSize = TextFieldAutoSize.NONE;
         this.addChild(this.__crutchTextField);
         this.__tempSprite = new Sprite();
         this.addChild(this.__tempSprite);
         this.__tempSprite.graphics.beginFill(3866392,0);
         this.__tempSprite.graphics.drawRect(0,0,100,100);
         this.__tempSprite.graphics.endFill();
         this.modFace = new NewPanelWithIcon(this.rightBox);
         this.modFace.paddingTop = 5;
         this.modFace.paddingLeft = 0;
         this.modFace.mouseEnabled = false;
         this.modFace.mouseChildren = false;
         this.modFace.title("extendedGUI.CharWindow.editChar",false);
         this.modFace.text("extendedGUI.CharWindow.editCharDescr",false);
         this.modFace.icon = new Bitmap(new face_change(),"auto",true);
         this.modFace.addEventListener("onCaptionClick",this.call_change_face);
         this.modItems = new NewPanelWithIcon(this.rightBox);
         this.modItems.paddingTop = 5;
         this.modItems.paddingLeft = 0;
         this.modItems.title("extendedGUI.CharWindow.editItems",false);
         this.modItems.text("extendedGUI.CharWindow.editItemsDescr",false);
         this.modItems.icon = new Bitmap(new items_change(),"auto",true);
         this.modItems.addEventListener("onCaptionClick",this.call_paint);
         this.premium = new NewPremiumPanel(this.rightBox);
         this.premium.paddingTop = 5;
         this.premium.paddingLeft = 0;
         this.surveyPanel = new SurveyPanel(this.rightBox);
         this.surveyPanel.paddingTop = 5;
         this.surveyPanel.paddingLeft = 0;
         this.surveyPanel.title("extendedGUI.CharWindow.editChar",false);
         this.surveyPanel.text("extendedGUI.CharWindow.editCharDescr",false);
         this.surveyPanel.icon = new Bitmap(new face_change(),"auto",true);
         this.surveyPanel.visible = false;
         this.surveyPanel.addEventListener("onCaptionClick",this.call_survey);
         var kostilpanel:EventsPanel = new EventsPanel(this.rightBox);
         kostilpanel.paddingTop = 5;
         kostilpanel.paddingLeft = 0;
         kostilpanel.clearListener();
         this.settings = new MenuButton2(this.vBox);
         this.settings.$ = "extendedGUI.RootWindow.settingsButton";
         this.settings.height = 35;
         this.settings.paddingTop = 10;
         this.settings.paddingLeft += 10;
         this.settings.addEventListener(MouseEvent.CLICK,this.onSettingsClick);
         this.partner = new MenuButton2(this.vBox);
         this.partner.$ = "extendedGUI.RootWindow.partnerButton";
         this.partner.height = 35;
         this.partner.paddingLeft = 10;
         this.partner.addEventListener(MouseEvent.CLICK,this.onPartnerClick);
         this.partner.enabled = Base.self.has_partner_id;
         this.promo = new MenuButton2(this.vBox);
         this.promo.$ = "extendedGUI.RootWindow.promoButton";
         this.promo.height = 35;
         this.promo.paddingLeft = 10;
         this.promo.addEventListener(MouseEvent.CLICK,this.onPromoClick);
         this.promo.enabled = true;
         this.support = new MenuButton2(this.vBox);
         this.support.$ = "extendedGUI.RootWindow.supportButton";
         this.support.height = 35;
         this.support.addEventListener(MouseEvent.CLICK,this.onSupportClick);
         this.support.paddingLeft = this.settings.paddingLeft;
         if(!Base.is_steam)
         {
            this.news = new MenuButton2(this.vBox);
            this.news.$ = "extendedGUI.RootWindow.newsButton";
            this.news.height = 35;
            this.news.addEventListener(MouseEvent.CLICK,this.onNewsClickHandler);
            this.news.paddingLeft = this.settings.paddingLeft;
            this.licenseBtn = new MenuButton2(this.vBox);
            this.licenseBtn.$ = "extendedGUI.RootWindow.licenseButton";
            this.licenseBtn.height = 35;
            this.licenseBtn.addEventListener(MouseEvent.CLICK,this.onLicenseClickHandler);
            this.licenseBtn.paddingLeft = this.settings.paddingLeft;
         }
         var quad2:Quad = new Quad(this.vBox);
         quad2.paddingLeft = 10;
         quad2.width = 413;
         quad2.height = 10;
         this.quit = new MenuButton2(this.vBox);
         this.quit.$ = "extendedGUI.RootWindow.quitButton";
         this.quit.height = 35;
         this.quit.addEventListener(MouseEvent.CLICK,this.onQuitClick);
         this.quit.paddingLeft = this.settings.paddingLeft;
         this.eventsPanel = new EventsPanel(this.vBox);
         this.eventsPanel.paddingTop = 5;
         this.eventsPanel.paddingLeft = 10;
         for(var i:int = 0; i < Character.MAX_COUNT; i++)
         {
            charBtn = new CharacterPanel2(this.charBox);
            charBtn.height = 37;
            charBtn.visible = true;
            charBtn.width = 413;
            charBtn.invalidate();
            this.charList.push(charBtn);
            charBtn.addEventListener(ScreenEvent.GO_SCREEN,this.onGoScreen);
         }
         this.rightBox.x = Base.stage.stageWidth - this.offsetChar;
         this.rightBox.y = 46;
         this.__crutchTextField.y = 46;
         this.__crutchTextField.x = this.rightBox.x + this.rightBox.width;
         this.__tempSprite.x = this.__crutchTextField.x;
         this.__tempSprite.y = this.__crutchTextField.y;
         this.__crutchTextField.width = Base.stage.stageWidth - this.__crutchTextField.x;
         this.__crutchTextField.addEventListener(MouseEvent.ROLL_OVER,this.onOverTemp);
         this.__crutchTextField.height = this.rightBox.height;
         setTimeout(this.updateToGameButton,100);
         this.setChildIndex(this.toGame,0);
      }
      
      protected function onOverTemp(event:MouseEvent) : *
      {
      }
      
      override protected function resize(... args) : void
      {
         Logger.LogToChannel(Logger.DEBUG,this,"resize");
         if(this.rightBox == null)
         {
            return;
         }
         if(Base.stage == null)
         {
            return;
         }
         this.rightBox.x = Base.stage.stageWidth - this.offsetChar;
         this.__crutchTextField.x = this.rightBox.x + this.rightBox.width;
         this.__crutchTextField.height = this.rightBox.height;
         this.__tempSprite.x = this.__crutchTextField.x;
         this.__tempSprite.height = this.rightBox.height;
      }
      
      protected function call_paint(event:Event) : *
      {
         Api.call(Api.DONAT_PAINT_CHAR);
      }
      
      protected function call_survey(event:Event) : *
      {
         Api.call(Api.CALL_SURVEY);
      }
      
      protected function call_change_face(event:Event) : *
      {
         this.modFace.mouseEnabled = false;
         setTimeout(Base.navigator.showCreateCharScreen,100,Base.navigator.getScreen(MainMenuGUI.ROOT_SCREEN),Character.current.name,true,true);
      }
      
      protected function onChangePartnerID(arg:ApiEvent) : *
      {
         if(this.partner != null)
         {
            this.partner.enabled = true;
         }
      }
      
      protected function showsurvey(arg:ApiEvent) : *
      {
         var title:String = arg.data.answer.title;
         var description:String = arg.data.answer.description;
         var icon_path:String = arg.data.answer.icon_path;
         var end_time:Number = Number(arg.data.answer.end_time);
         IconLoader.loadIconByPath(icon_path,this.onLoadSurveyIcon);
         this.surveyPanel.mouseEnabled = true;
         this.surveyPanel.mouseChildren = true;
         this.surveyPanel.time_end(end_time);
         this.surveyPanel.title(title,false);
         this.surveyPanel.text(description,false);
         this.surveyPanel.visible = false;
      }
      
      protected function hidesurvey(arg:ApiEvent) : *
      {
         if(this.surveyPanel)
         {
            this.surveyPanel.visible = false;
         }
      }
      
      protected function onLoadSurveyIcon(icon:BitmapData) : *
      {
         this.surveyPanel.icon = new Bitmap(icon,"auto",true);
         this.surveyPanel.visible = true;
      }
      
      protected function onChangeEditBtnEnabled(arg:ApiEvent) : *
      {
         var value:Boolean = Boolean(arg.data.answer.value);
         CharacterPanel2.EDIT_ENABLED = value;
         this.modFace.mouseEnabled = value;
         this.modFace.mouseChildren = value;
         for(var i:uint = 0; i < Character.MAX_COUNT; i++)
         {
            char = Character.list[i] as Character;
            (this.charList[i] as CharacterPanel2).set_character(char);
            if(char != null)
            {
               (this.charList[i] as CharacterPanel2).edidCharBtnEnabled(value);
            }
         }
      }
      
      protected function onGoScreen(event:ScreenEvent) : void
      {
         this.dispatchEvent(event);
      }
      
      protected function onCharacterChange(event:Event) : void
      {
         this.updateToGameButton();
         this.updateContent();
      }
      
      protected function onAllCharInfoHandler(arg1:ApiEvent) : *
      {
         if(Character != null)
         {
            Character.onAllCharInfoHandler(arg1);
         }
      }
      
      protected function onCharactersUpdated(event:Event) : void
      {
         this.updateToGameButton();
         this.updateContent();
      }
      
      protected function updateContent() : void
      {
         var before:int = int(getTimer());
         for(var i:uint = 0; i < Character.MAX_COUNT; i++)
         {
            char = Character.list[i] as Character;
            if(char != null)
            {
               this.charList[i].selected = char.id == Character.currentId;
            }
            this.charList[i].set_character(char);
         }
         this.charBox.invalidate();
         this.charBox.draw();
      }
      
      protected function updateToGameButton() : void
      {
         this.toGame.enabled = this.startAllowed;
         if(Character.current != null && Boolean(Character.current.is_old) && Base.navigator.currentScreen == this)
         {
            setTimeout(this.showUpdateCharDialog,200);
         }
      }
      
      protected function get startAllowed() : Boolean
      {
         return Character.list.length > 0 && Character.current != null && Boolean(Character.current.isNotDeleted);
      }
      
      private function showUpdateCharDialog(... args) : *
      {
         var scr:Screen = Base.navigator.getScreen(MainMenuGUI.DONATE_INFO_SCREEN);
         if(Boolean(scr) && Boolean(Base.navigator.getScreen(MainMenuGUI.DONATE_INFO_SCREEN).vis_pbt))
         {
            return;
         }
         if(this.stupid_flag_for_DAUN_ANTON)
         {
            return;
         }
         if(!(Character.current != null && Boolean(Character.current.is_old) && Base.navigator.currentScreen == this))
         {
            return;
         }
         this.stupid_flag_for_DAUN_ANTON = true;
         Base.navigator.showDialog("extendedGUI.RootWindow.charOldUpdateTitle","extendedGUI.RootWindow.charOldUpdate",true,[new DialogButtonItem("extendedGUI.Dialogs.Yes",this.startUpdateCharacter,0.4,[Keyboard.ENTER]),new DialogButtonItem("extendedGUI.Dialogs.No",this.doGoBack,0.6,[Keyboard.ESCAPE])],500,300,false,true,true);
      }
      
      protected function onToGameClick(event:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"MainScreen.onToGameClick");
         if(Character.current.is_old)
         {
            this.showUpdateCharDialog();
         }
         else
         {
            this.playWithTutorial();
         }
      }
      
      protected function onToGameOver(event:MouseEvent) : void
      {
         this.toGameBackGround.alpha = 1;
      }
      
      protected function onToGameOut(event:MouseEvent) : void
      {
         this.toGameBackGround.alpha = 0.5;
      }
      
      private function askTutorialIfNeed() : void
      {
         if(Character.EveryoneNotPassed)
         {
            this.playWithTutorial();
         }
         else if(Character.CurrentNotPassed)
         {
            Base.navigator.showDialog("extendedGUI.RootWindow.askTutorialHeader","extendedGUI.RootWindow.askTutorialQuestion",true,[new DialogButtonItem("extendedGUI.Dialogs.Yes",this.playWithTutorial,0.4,[Keyboard.ENTER]),new DialogButtonItem("extendedGUI.Dialogs.No",this.playWithoutTutorial,0.6,[Keyboard.ESCAPE])],500,200);
         }
         else
         {
            this.playWithoutTutorial();
         }
      }
      
      private function startUpdateCharacter(... args) : void
      {
         this.stupid_flag_for_DAUN_ANTON = false;
         Base.navigator.showCreateCharScreen(this,Character.current.name,true);
      }
      
      private function playWithoutTutorial() : void
      {
         Api.call(Api.GO_TO_GAME,[{"tutorial_passed":1}]);
      }
      
      private function playWithTutorial() : void
      {
         this.removeEventListener(KeyboardEvent.KEY_DOWN,this.onEnterDown);
         Api.call(Api.GO_TO_GAME,[{"tutorial_passed":0}]);
      }
      
      protected function onNewsClickHandler(event:Event) : void
      {
         Api.call(Api.TRY_SHOW_NEWS);
      }
      
      protected function onLicenseClickHandler(event:Event) : void
      {
         var eulaText:* = Locale.getById("EULA.eula_text.topic") + "\n\n" + Locale.getById("EULA.eula_text.paragraph") + "\n\n" + Locale.getById("EULA.eula_text.resume") + "\n\n";
         Base.navigator.showDialog("EULA ",eulaText,true,[new DialogButtonItem("extendedGUI.NewsWindow.closeDialog",null,1,[Keyboard.ENTER,Keyboard.ESCAPE])],800,500);
      }
      
      protected function onSettingsClick(event:MouseEvent) : void
      {
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.BASE_SETTINGS));
      }
      
      protected function onAccountClick(event:MouseEvent) : void
      {
         Api.call(Api.OPEN_URL,[{"url":"https://www.stalker.so/kabinet"}]);
      }
      
      protected function onPartnerClick(event:MouseEvent) : void
      {
         Api.call("on_affiliate_program_click");
      }
      
      protected function onPromoClick(event:MouseEvent) : void
      {
         Api.call("on_promo_click");
      }
      
      protected function onCutSceneClick(event:MouseEvent) : void
      {
         Api.call("on_cutscene_click");
      }
      
      protected function onSupportClick(event:MouseEvent) : void
      {
         Api.call(Api.OPEN_URL,[{"url":Locale.current.supportUrl}]);
      }
      
      protected function onQuitClick(event:MouseEvent) : void
      {
         this.goBack();
      }
      
      override public function goBack() : void
      {
         if(Base.SHOP_OPENING)
         {
            return;
         }
         Base.navigator.showDialog("extendedGUI.Dialogs.Warning","extendedGUI.RootWindow.quitMessage",true,[new DialogButtonItem("extendedGUI.Dialogs.Yes",this.doGoBack,0.4,[Keyboard.ENTER]),new DialogButtonItem("extendedGUI.Dialogs.No",null,0.6,[Keyboard.ESCAPE])],500,200);
      }
      
      protected function doGoBack() : void
      {
         this.stupid_flag_for_DAUN_ANTON = false;
         Base.navigator.header.newsVisible = false;
         Auth.self.doLogout();
      }
      
      override public function hideUp(onCompleteFunction:Function = null) : void
      {
         super.hideUp(onCompleteFunction);
         this.removeEventListener(KeyboardEvent.KEY_DOWN,this.onEnterDown);
         Base.navigator.header.newsVisible = false;
      }
      
      override public function hideDown(onCompleteFunction:Function = null) : void
      {
         super.hideDown(onCompleteFunction);
         this.removeEventListener(KeyboardEvent.KEY_DOWN,this.onEnterDown);
         Base.navigator.header.newsVisible = false;
      }
      
      override public function showUp() : void
      {
         super.showUp();
         Base.navigator.header.account.invalidate();
         this.addEventListener(KeyboardEvent.KEY_DOWN,this.onEnterDown);
      }
      
      override public function showDown() : void
      {
         super.showDown();
         Base.navigator.header.account.invalidate();
         this.addEventListener(KeyboardEvent.KEY_DOWN,this.onEnterDown);
      }
      
      public function checkUpdateCharDialog() : *
      {
         if(Character.current != null && Boolean(Character.current.is_old))
         {
            this.showUpdateCharDialog();
         }
      }
      
      private function onEnterDown(e:KeyboardEvent) : *
      {
         if(!this.startAllowed)
         {
            return;
         }
         if(e.keyCode == Keyboard.ENTER || e.keyCode == Keyboard.NUMPAD_ENTER)
         {
            this.onToGameClick(null);
         }
      }
   }
}

