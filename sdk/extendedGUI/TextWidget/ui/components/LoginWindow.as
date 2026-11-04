package ui.components
{
   import com.dvalimona.components.*;
   import com.dvalimona.utils.*;
   import com.greensock.*;
   import com.greensock.easing.*;
   import communication.*;
   import events.ApiEvent;
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   import flash.ui.*;
   import flash.utils.*;
   import lang.*;
   import logging.*;
   
   public class LoginWindow extends Window
   {
      internal const ERROR_COLOR:* = 16711680;
      
      protected var regButton:ClearButton;
      
      protected var orLabel:Label;
      
      protected var errorLabel:Label;
      
      protected var serverBox:HBox;
      
      protected var serverBoxHeight:uint = 90;
      
      protected var serverLabel:Label;
      
      protected var serverCombo:ComboBox;
      
      protected var serverItems:Object;
      
      protected var includeDeveloperServers:Boolean = false;
      
      protected var loginBox:HBox;
      
      protected var loginBoxHeight:uint = 50;
      
      protected var loginLabel:Label;
      
      protected var loginInput:InputText;
      
      protected var passwordBox:HBox;
      
      protected var passwordBoxHeight:uint = 50;
      
      protected var passwordLabel:Label;
      
      protected var passwordInput:InputText;
      
      protected var tabStops:Array = [0,120];
      
      protected var rememberLoginBox:HBox;
      
      protected var rememberLoginBoxHeight:uint = 50;
      
      protected var rememberLoginLabel:Label;
      
      protected var rememberLoginCheck:CheckBox;
      
      protected var buttonsBox:HBox;
      
      protected var buttonsBoxHeight:uint = 60;
      
      protected var demoButton:PushButton;
      
      protected var loginButton:PushButton;
      
      public function LoginWindow(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0, param4:String = "Window")
      {
         super(param1,param2,param3,param4);
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         this.headerDeals();
         this.bodyDeals();
      }
      
      private function bodyDeals() : void
      {
         this.serverBox = new HBox();
         this.serverBox.debug = false;
         this.serverBox.alignment = HBox.MIDDLE;
         this.serverBox.horizontalAlign = HBox.LEFT;
         this.serverBox.fixedHeight = this.serverBoxHeight;
         this.serverBox.tabStops = this.tabStops;
         super.addChild(this.serverBox);
         this.serverLabel = new Label();
         this.serverLabel.debug = false;
         this.serverLabel.autoSize = true;
         this.serverLabel.$ = "extendedGUI.LoginWindow.Server";
         this.serverLabel.size = 20;
         this.serverBox.addChild(this.serverLabel);
         this.serverCombo = new ComboBox(this.serverBox);
         this.serverCombo.listItemHeight = 40;
         this.serverCombo.spacing = 1;
         this.serverCombo.addItem({
            "label":"label0",
            "id":"0"
         });
         this.serverCombo.addItem("Много текста очень много, очень очень. Очень. Много.");
         this.serverCombo.addItem("tea2");
         this.serverCombo.addItem("tea3");
         this.serverCombo.addItem("tea4");
         this.serverCombo.addItem("tea5");
         this.serverCombo.setSize(300,40);
         this.serverCombo.numVisibleItems = Math.min(this.serverCombo.items.length,5);
         this.serverCombo.addEventListener(Event.SELECT,this.onComboSelect);
         this.loginBox = new HBox();
         this.loginBox.alignment = HBox.MIDDLE;
         this.loginBox.fixedHeight = this.loginBoxHeight;
         this.loginBox.tabStops = this.tabStops;
         super.addChild(this.loginBox);
         this.loginLabel = new Label();
         this.loginLabel.autoSize = true;
         this.loginLabel.debug = false;
         this.loginLabel.$ = "extendedGUI.LoginWindow.Login";
         this.loginLabel.size = 20;
         this.loginBox.addChild(this.loginLabel);
         this.loginInput = new InputText();
         this.loginInput.setSize(300,40);
         this.loginInput.size = 22;
         this.loginInput.padding = 0;
         this.loginInput.addEventListener(Event.CHANGE,this.onLoginChanged);
         this.loginInput.textField.addEventListener(KeyboardEvent.KEY_DOWN,this.onLoginKeydown);
         this.loginBox.addChild(this.loginInput);
         this.passwordBox = new HBox();
         this.passwordBox.alignment = HBox.MIDDLE;
         this.passwordBox.fixedHeight = this.passwordBoxHeight;
         this.passwordBox.tabStops = this.tabStops;
         super.addChild(this.passwordBox);
         this.passwordLabel = new Label();
         this.passwordLabel.autoSize = true;
         this.passwordLabel.debug = false;
         this.passwordLabel.$ = "extendedGUI.LoginWindow.Password";
         this.passwordLabel.size = 20;
         this.passwordBox.addChild(this.passwordLabel);
         this.passwordInput = new InputText();
         this.passwordInput.setSize(300,40);
         this.passwordInput.size = 14;
         this.passwordInput.padding = 0;
         this.passwordInput.password = true;
         this.passwordInput.font = Base.FONT_BULLETS;
         this.passwordInput.addEventListener(Event.CHANGE,this.onPasswordChanged);
         this.passwordInput.textField.addEventListener(KeyboardEvent.KEY_DOWN,this.onPasswordKeydown);
         this.passwordBox.addChild(this.passwordInput);
         this.errorLabel = new Label();
         this.errorLabel.autoSize = true;
         this.errorLabel.debug = false;
         this.errorLabel.$ = "extendedGUI.LoginWindow.Error";
         this.errorLabel.size = 16;
         this.errorLabel.textField.textColor = this.ERROR_COLOR;
         this.addChild(this.errorLabel);
         this.errorLabel.visible = false;
         this.rememberLoginBox = new HBox();
         this.rememberLoginBox.alignment = HBox.MIDDLE;
         this.rememberLoginBox.fixedHeight = this.rememberLoginBoxHeight;
         this.rememberLoginBox.horizontalAlign = HBox.RIGHT;
         this.rememberLoginBox.debug = false;
         super.addChild(this.rememberLoginBox);
         this.rememberLoginCheck = new CheckBox();
         this.rememberLoginCheck.debug = false;
         this.rememberLoginCheck.focusMarginX = 6;
         this.rememberLoginCheck.focusMarginY = 6;
         this.rememberLoginCheck.tabEnabled = true;
         this.rememberLoginBox.addChild(this.rememberLoginCheck);
         this.rememberLoginLabel = new Label();
         this.rememberLoginLabel.autoSize = true;
         this.rememberLoginLabel.debug = false;
         this.rememberLoginLabel.$ = "extendedGUI.LoginWindow.RememberLogin";
         this.rememberLoginLabel.size = 20;
         this.rememberLoginBox.addChild(this.rememberLoginLabel);
         this.buttonsBox = new HBox();
         this.buttonsBox.alignment = HBox.MIDDLE;
         this.buttonsBox.fixedHeight = this.buttonsBoxHeight;
         this.buttonsBox.fixedWidth = 460;
         this.buttonsBox.horizontalAlign = HBox.LEFT;
         this.buttonsBox.spacing = 1;
         this.buttonsBox.debug = false;
         super.addChild(this.buttonsBox);
         this.demoButton = new PushButton();
         this.demoButton.focusMarginX = 0;
         this.demoButton.focusMarginY = 0;
         this.demoButton.font = Base.FONT_LIGHT;
         this.demoButton.size = 22;
         this.demoButton.$ = "extendedGUI.LoginWindow.DemoGame";
         this.demoButton.labelUpColor = 5626367;
         this.demoButton.labelOverColor = 10347511;
         this.demoButton.overColorAlpha = 1;
         this.demoButton.setSize(150,this.buttonsBoxHeight);
         this.demoButton.addEventListener(MouseEvent.CLICK,this.onDemoClickHandler);
         this.loginButton = new PushButton();
         this.loginButton.focusMarginX = 0;
         this.loginButton.focusMarginY = 0;
         this.loginButton.addEventListener(MouseEvent.CLICK,this.onLoginClickHandler);
         this.loginButton.font = Base.FONT_BOLD;
         this.loginButton.size = 22;
         this.loginButton.$ = "extendedGUI.LoginWindow.AuthButton";
         this.loginButton.setSize(309 + 151,this.buttonsBoxHeight);
         this.loginButton.overColorAlpha = 1;
         this.buttonsBox.addChild(this.loginButton);
         this.serverDeals();
         this.updateLoginButton();
      }
      
      protected function onShowError() : *
      {
         var f:Function;
         this.errorLabel.x = this.loginBox.x + this.loginInput.x + 10;
         this.errorLabel.y = this.loginBox.y - 20;
         this.errorLabel.visible = true;
         f = function():*
         {
            errorLabel.visible = false;
         };
         setTimeout(f,3000);
      }
      
      protected function validateLogin() : *
      {
         if(Boolean(StringUtils.validateString(this.passwordInput.text)) && Boolean(StringUtils.validateString(this.loginInput.text)))
         {
            Base.navigator.focus.target = null;
            Logger.LogToChannel(Logger.DEBUG,"onLoginClickHandler");
            setTimeout(this.doLogin,1);
         }
         else
         {
            this.onShowError();
         }
      }
      
      protected function onPasswordKeydown(param1:KeyboardEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onPasswordKeydown",param1.keyCode,Keyboard.NUMPAD_ENTER,Keyboard.ENTER);
         if(param1.keyCode == Keyboard.NUMPAD_ENTER || param1.keyCode == Keyboard.ENTER)
         {
            if(this.serverCombo.selectedIndex >= 0 && this.loginInput.text.length && Boolean(this.passwordInput.text.length))
            {
               this.validateLogin();
            }
         }
      }
      
      protected function onLoginKeydown(param1:KeyboardEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onLoginKeydown",param1.keyCode,Keyboard.NUMPAD_ENTER,Keyboard.ENTER);
         if(param1.keyCode == Keyboard.NUMPAD_ENTER || param1.keyCode == Keyboard.ENTER)
         {
            this.setPasswordFocus();
         }
      }
      
      protected function setLoginFocus() : void
      {
         Base.stage.focus = this.loginInput.textField;
         this.loginInput.textField.setSelection(this.loginInput.textField.length,this.loginInput.textField.length);
      }
      
      protected function setPasswordFocus() : void
      {
         Base.stage.focus = this.passwordInput.textField;
         this.passwordInput.textField.setSelection(this.passwordInput.textField.length,this.passwordInput.textField.length);
      }
      
      protected function onDemoClickHandler(param1:MouseEvent) : void
      {
         Base.navigator.showDialog("extendedGUI.Dialogs.Warning","extendedGUI.LoginWindow.NoDemoText",true,[new DialogButtonItem("extendedGUI.Dialogs.Ok",null,1)]);
      }
      
      protected function onLoginClickHandler(param1:MouseEvent) : void
      {
         this.validateLogin();
      }
      
      protected function doLogin() : void
      {
         Base.navigator.currentServer = this.serverCombo.selectedItem.label;
         Auth.self.doLogin({
            "login":this.loginInput.text,
            "password":this.passwordInput.text,
            "rememberMe":(!!this.rememberLoginCheck.selected ? 1 : 0),
            "serverID":this.serverCombo.selectedItem.id
         });
      }
      
      public function restoreMe() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"LoginWindow.restoreMe");
         this.enabled = true;
      }
      
      override public function set enabled(param1:Boolean) : void
      {
         if(param1)
         {
            this.enableMe();
         }
         else
         {
            this.disableMe();
         }
      }
      
      protected function enableMe() : void
      {
         TweenMax.killTweensOf(this);
         TweenMax.to(this,0.5,{
            "alpha":1,
            "ease":Expo.easeOut
         });
         this.loginButton.enabled = true;
         this.updateLoginButton();
      }
      
      protected function disableMe() : void
      {
         TweenMax.killTweensOf(this);
         TweenMax.to(this,0.5,{
            "alpha":0.2,
            "ease":Expo.easeOut
         });
         this.loginButton.enabled = false;
      }
      
      protected function onPasswordChanged(param1:Event) : void
      {
         this.updateLoginButton();
      }
      
      protected function onLoginChanged(param1:Event) : void
      {
         this.updateLoginButton();
      }
      
      protected function onComboSelect(param1:Event) : void
      {
         this.updateLoginButton();
      }
      
      private function serverDeals() : void
      {
         Api.self.addEventListener(Api.SHOW_DEVELOPER_SERVERS,this.onShowDevServersHandler);
         Api.self.addEventListener(Api.GET_SERVER_LIST,this.onServerListHandler);
         Api.call(Api.GET_SERVER_LIST);
         Api.self.addEventListener(Api.GET_LOGIN,this.onGetLoginHandler);
         Api.call(Api.GET_LOGIN);
      }
      
      protected function onGetLoginHandler(param1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_LOGIN,this.onGetLoginHandler);
         Logger.LogToChannel(Logger.DEBUG,param1.data.name,param1.data.answer.login);
         if(Boolean(param1.data.answer.login) && Boolean(param1.data.answer.login.length))
         {
            this.loginInput.text = param1.data.answer.login;
            this.rememberLoginCheck.selected = true;
            setTimeout(this.setPasswordFocus,100);
            setTimeout(this.setPasswordFocus,200);
            setTimeout(this.setPasswordFocus,500);
         }
         else
         {
            this.loginInput.text = "";
            this.rememberLoginCheck.selected = false;
            setTimeout(this.setLoginFocus,100);
         }
      }
      
      protected function onShowDevServersHandler(param1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,param1.data.name);
         this.includeDeveloperServers = !this.includeDeveloperServers;
         this.fillServerCombo();
      }
      
      protected function onServerListHandler(param1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_SERVER_LIST,this.onServerListHandler);
         Logger.LogToChannel(Logger.DEBUG,param1.data.answer.list);
         this.serverItems = param1.data;
         this.fillServerCombo();
      }
      
      protected function fillServerCombo() : void
      {
         var _loc4_:Object = null;
         var _loc5_:Object = null;
         var _loc6_:ServerItem = null;
         var _loc7_:ServerItem = null;
         Logger.LogToChannel(Logger.DEBUG,"fillServerCombo");
         this.serverCombo.removeAll();
         var _loc1_:int = -1;
         var _loc2_:uint = 0;
         var _loc3_:Boolean = Boolean(this.includeDeveloperServers);
         for each(_loc4_ in this.serverItems.answer.list)
         {
            _loc6_ = new ServerItem(_loc4_);
            if(String(this.serverItems.answer.defaultServerId) == _loc6_.id && _loc6_.isDevelop)
            {
               _loc3_ = true;
            }
         }
         for each(_loc5_ in this.serverItems.answer.list)
         {
            _loc7_ = new ServerItem(_loc5_);
            Logger.LogToChannel(Logger.DEBUG,_loc7_,_loc7_.id,_loc7_.label);
            if(_loc7_.isDevelop)
            {
               if(_loc3_)
               {
                  this.serverCombo.addItem(_loc7_);
               }
            }
            else
            {
               this.serverCombo.addItem(_loc7_);
            }
            if(String(this.serverItems.answer.defaultServerId) == _loc7_.id)
            {
               _loc1_ = int(_loc2_);
            }
            _loc2_++;
         }
         this.serverCombo.selectedIndex = _loc1_;
         this.serverCombo.setSize(300,40);
         this.serverCombo.numVisibleItems = Math.min(this.serverCombo.items.length,5);
      }
      
      private function headerDeals() : void
      {
         header.tabEnabled = false;
         header.tabChildren = true;
         leftItems.shift = 3;
         _titleLabel.autoSize = true;
         _titleLabel.font = Base.FONT_BOLD;
         _titleLabel.$ = "extendedGUI.LoginWindow.GetAuthorize";
         _titleLabel.size = 20;
         _titleLabel.mouseEnabled = false;
         _titleLabel.mouseChildren = false;
         this.orLabel = new Label();
         this.orLabel.$ = "extendedGUI.LoginWindow.or";
         this.orLabel.size = 20;
         this.orLabel.color = 8881541;
         leftItems.addChild(this.orLabel);
         this.regButton = new ClearButton();
         this.regButton.$ = "extendedGUI.LoginWindow.GetReg";
         this.regButton.addEventListener(MouseEvent.CLICK,this.onRegHandler);
         this.regButton.setSize(200,30);
         this.regButton.size = 20;
         this.regButton.underline = false;
         this.regButton.autoWidth = true;
         this.regButton.tabEnabled = true;
         leftItems.addChild(this.regButton);
      }
      
      protected function onRegHandler(param1:Event) : void
      {
         Api.call(Api.OPEN_URL,[{"url":Locale.current.registrationUrl}]);
      }
      
      private function updateLoginButton() : void
      {
         this.loginButton.enabled = this.serverCombo.selectedIndex >= 0 && Boolean(this.loginInput.text.length) && Boolean(this.passwordInput.text.length);
      }
      
      override public function draw() : void
      {
         super.draw();
         this.serverBox.x = sideMargin;
         this.serverBox.setSize(width - sideMargin * 2,this.serverBox.height);
         this.serverCombo.listShift = -this.serverCombo.x - sideMargin;
         this.serverCombo.listWidth = width;
         this.loginBox.x = sideMargin;
         this.loginBox.y = this.serverBox.y + this.serverBox.height;
         this.loginBox.setSize(width - sideMargin * 2,this.loginBox.height);
         this.passwordBox.x = sideMargin;
         this.passwordBox.y = this.loginBox.y + this.loginBox.height;
         this.passwordBox.setSize(width - sideMargin * 2,this.passwordBox.height);
         this.rememberLoginBox.x = sideMargin;
         this.rememberLoginBox.y = this.passwordBox.y + this.passwordBox.height;
         this.rememberLoginBox.setSize(width - sideMargin * 2,this.rememberLoginBox.height);
         this.rememberLoginBox.fixedWidth = width - sideMargin * 2;
         this.buttonsBox.setSize(width,this.buttonsBoxHeight);
         this.buttonsBox.x = 0;
         this.buttonsBox.y = height - this.buttonsBoxHeight;
      }
   }
}

