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
      
      public function LoginWindow(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, title:String = "Window")
      {
         super(parent,xpos,ypos,title);
         Api.self.addEventListener(Api.PASSWORD_TEXT,this.onRecievePasswordText);
         super.addEventListener(MouseEvent.MOUSE_DOWN,this.onMouseDown2);
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
         this.loginLabel.visible = !Base.is_steam;
         this.loginBox.addChild(this.loginLabel);
         this.loginInput = new InputText();
         this.loginInput.setSize(300,40);
         this.loginInput.size = 22;
         this.loginInput.padding = 0;
         this.loginInput.addEventListener(Event.CHANGE,this.onLoginChanged);
         this.loginInput.textField.addEventListener(KeyboardEvent.KEY_DOWN,this.onLoginKeydown);
         this.loginInput.visible = !Base.is_steam;
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
         this.passwordInput.font = Base.bulletsFontName;
         this.passwordInput.enabled = !Base.is_steam;
         this.passwordInput.addEventListener(Event.CHANGE,this.onPasswordChanged);
         this.passwordInput.textField.addEventListener(KeyboardEvent.KEY_DOWN,this.onPasswordKeydown);
         this.passwordBox.addChild(this.passwordInput);
         this.errorLabel = new Label();
         this.errorLabel.autoSize = true;
         this.errorLabel.debug = false;
         this.errorLabel.$ = "extendedGUI.LoginWindow.ErrorLogin";
         this.errorLabel.size = 16;
         this.errorLabel.textField.textColor = this.ERROR_COLOR;
         if(!Base.is_steam)
         {
            this.addChild(this.errorLabel);
         }
         this.errorLabel.visible = false;
         this.rememberLoginBox = new HBox();
         this.rememberLoginBox.alignment = HBox.MIDDLE;
         this.rememberLoginBox.fixedHeight = this.rememberLoginBoxHeight;
         this.rememberLoginBox.horizontalAlign = HBox.RIGHT;
         this.rememberLoginBox.debug = false;
         if(!Base.is_steam)
         {
            super.addChild(this.rememberLoginBox);
         }
         this.rememberLoginCheck = new CheckBox();
         this.rememberLoginCheck.debug = false;
         this.rememberLoginCheck.focusMarginX = 6;
         this.rememberLoginCheck.focusMarginY = 6;
         this.rememberLoginCheck.tabEnabled = true;
         this.rememberLoginCheck.addEventListener(Event.CHANGE,this.onRememberCheck);
         this.rememberLoginBox.addChild(this.rememberLoginCheck);
         this.rememberLoginLabel = new Label();
         this.rememberLoginLabel.autoSize = true;
         this.rememberLoginLabel.debug = false;
         this.rememberLoginLabel.color = 16711680;
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
         this.demoButton.font = Base.lightFontName;
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
         this.loginButton.font = Base.boldFontName;
         this.loginButton.size = 22;
         this.loginButton.$ = "extendedGUI.LoginWindow.AuthButton";
         this.loginButton.setSize(309 + 151,this.buttonsBoxHeight);
         this.loginButton.overColorAlpha = 1;
         this.buttonsBox.addChild(this.loginButton);
         this.serverDeals();
         this.updateLoginButton();
      }
      
      protected function onMouseDown2(event:MouseEvent) : void
      {
         if(this.serverCombo.isOpen)
         {
            this.serverCombo.removeList();
         }
      }
      
      protected function onRememberCheck(event:Event) : *
      {
         Api.call(Api.REMEMBER_PASS_CHECK,[this.loginInput.text,this.passwordInput.text,this.rememberLoginCheck.selected]);
      }
      
      protected function onRecievePasswordText(event:ApiEvent) : *
      {
         if(this.passwordInput == null)
         {
            return;
         }
         this.passwordInput.text = event.data.answer.text;
         this.passwordInput.textField.setSelection(this.passwordInput.textField.length,this.passwordInput.textField.length);
         this.onPasswordChanged(null);
      }
      
      protected function onShowError(key_code:Number) : *
      {
         var f:Function;
         switch(key_code)
         {
            case 0:
               this.errorLabel.$ = "extendedGUI.LoginWindow.ErrorLogin";
               break;
            case 1:
               this.errorLabel.$ = "extendedGUI.LoginWindow.ErrorEmail";
         }
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
            this.doLogin();
         }
         else
         {
            this.onShowError(0);
         }
      }
      
      protected function onPasswordKeydown(event:KeyboardEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onPasswordKeydown",event.keyCode,Keyboard.NUMPAD_ENTER,Keyboard.ENTER);
         if(event.keyCode == Keyboard.NUMPAD_ENTER || event.keyCode == Keyboard.ENTER)
         {
            if(this.serverCombo.selectedIndex >= 0 && this.loginInput.text.length && Boolean(this.passwordInput.text.length))
            {
               this.validateLogin();
            }
         }
      }
      
      protected function onLoginKeydown(event:KeyboardEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onLoginKeydown",event.keyCode,Keyboard.NUMPAD_ENTER,Keyboard.ENTER,Keyboard.TAB);
         if(event.keyCode == Keyboard.NUMPAD_ENTER || event.keyCode == Keyboard.ENTER || event.keyCode == Keyboard.TAB)
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
      
      protected function onDemoClickHandler(event:MouseEvent) : void
      {
         Base.navigator.showDialog("extendedGUI.Dialogs.Warning","extendedGUI.LoginWindow.NoDemoText",true,[new DialogButtonItem("extendedGUI.Dialogs.Ok",null,1)]);
      }
      
      protected function onLoginClickHandler(event:MouseEvent) : void
      {
         if(Base.is_steam)
         {
            this.doLogin();
         }
         else
         {
            this.validateLogin();
         }
      }
      
      protected function doLogin() : void
      {
         Base.navigator.currentServer = this.serverCombo.selectedItem.label;
         Base.navigator.currentLogin = this.loginInput.text;
         Auth.self.doLogin({
            "login":this.loginInput.text,
            "password":this.passwordInput.text,
            "rememberMe":(this.rememberLoginCheck.selected ? 1 : 0),
            "serverID":this.serverCombo.selectedItem.id
         });
      }
      
      public function restoreMe() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"LoginWindow.restoreMe");
         this.enabled = true;
      }
      
      override public function set enabled(value:Boolean) : void
      {
         if(value)
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
      
      protected function onPasswordChanged(event:Event) : void
      {
         this.updateLoginButton();
      }
      
      protected function onLoginChanged(event:Event) : void
      {
         this.updateLoginButton();
      }
      
      protected function onComboSelect(event:Event) : void
      {
         this.updateLoginButton();
      }
      
      private function serverDeals() : void
      {
         Api.self.addEventListener(Api.SHOW_DEVELOPER_SERVERS,this.onShowDevServersHandler);
         Api.self.addEventListener(Api.GET_SERVER_LIST,this.onServerListHandler);
         Api.call(Api.GET_SERVER_LIST);
         Api.self.addEventListener(Api.GET_LOGIN,this.onGetLoginHandler);
         Api.self.addEventListener(Api.GET_REMEMBER_PASS,this.onGetRememberCheck);
         Api.call(Api.GET_LOGIN);
      }
      
      protected function onGetLoginHandler(event:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_LOGIN,this.onGetLoginHandler);
         Logger.LogToChannel(Logger.DEBUG,event.data.name,event.data.answer.login);
         if(Boolean(event.data.answer.login) && Boolean(event.data.answer.login.length))
         {
            this.loginInput.text = event.data.answer.login;
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
         this.updateLoginButton();
      }
      
      protected function onGetRememberCheck(event:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_REMEMBER_PASS,this.onGetRememberCheck);
         this.rememberLoginCheck.selected = event.data.answer.value;
      }
      
      protected function onShowDevServersHandler(event:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,event.data.name);
         this.includeDeveloperServers = !this.includeDeveloperServers;
         this.passwordBox.fixedHeight = this.passwordBoxHeight;
         this.passwordBox.visible = true;
         this.passwordBox.setSize(width - sideMargin * 2,this.passwordBoxHeight);
         this.passwordInput.visible = true;
         this.passwordInput.enabled = true;
         this.passwordLabel.visible = true;
         this.loginInput.visible = true;
         this.loginLabel.visible = true;
         if(Base.is_steam)
         {
            this.loginLabel.text = "SteamID";
         }
         this.height = 350;
         this.fillServerCombo();
         invalidate();
         this.passwordBox.invalidate();
      }
      
      protected function onServerListHandler(event:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_SERVER_LIST,this.onServerListHandler);
         Logger.LogToChannel(Logger.DEBUG,event.data.answer.list);
         this.serverItems = event.data;
         this.fillServerCombo();
      }
      
      protected function fillServerCombo() : void
      {
         var tobj:Object = null;
         var obj:Object = null;
         var serverItem:ServerItem = null;
         var newItem:ServerItem = null;
         Logger.LogToChannel(Logger.DEBUG,"fillServerCombo");
         this.serverCombo.removeAll();
         var selectedIndex:int = -1;
         var count:uint = 0;
         var needIncludeDevServers:Boolean = Boolean(this.includeDeveloperServers);
         for each(tobj in this.serverItems.answer.list)
         {
            serverItem = new ServerItem(tobj);
            if(String(this.serverItems.answer.defaultServerId) == serverItem.id && serverItem.isDevelop)
            {
               needIncludeDevServers = true;
            }
         }
         for each(obj in this.serverItems.answer.list)
         {
            newItem = new ServerItem(obj);
            Logger.LogToChannel(Logger.DEBUG,newItem,newItem.id,newItem.label);
            if(newItem.isDevelop)
            {
               if(needIncludeDevServers)
               {
                  this.serverCombo.addItem(newItem);
               }
            }
            else
            {
               this.serverCombo.addItem(newItem);
            }
            if(String(this.serverItems.answer.defaultServerId) == newItem.id)
            {
               selectedIndex = int(count);
            }
            count++;
         }
         this.serverCombo.selectedIndex = selectedIndex;
         if(selectedIndex == -1)
         {
            this.serverCombo.text = Locale.getById("extendedGUI.LoginWindow.noSelect");
         }
         this.serverCombo.setSize(300,40);
         this.serverCombo.numVisibleItems = Math.min(this.serverCombo.items.length,5);
      }
      
      private function headerDeals() : void
      {
         header.tabEnabled = false;
         header.tabChildren = true;
         leftItems.shift = 3;
         _titleLabel.autoSize = true;
         _titleLabel.font = Base.boldFontName;
         _titleLabel.$ = "extendedGUI.LoginWindow.GetAuthorize";
         if(Base.is_steam)
         {
            _titleLabel.$ = "extendedGUI.LoginWindow.GameName";
            _titleLabel.color = 8881541;
         }
         _titleLabel.size = 20;
         _titleLabel.mouseEnabled = false;
         _titleLabel.mouseChildren = false;
         this.orLabel = new Label();
         this.orLabel.$ = "extendedGUI.LoginWindow.or";
         this.orLabel.size = 20;
         this.orLabel.color = 8881541;
         if(!Base.is_steam)
         {
            leftItems.addChild(this.orLabel);
         }
         this.regButton = new ClearButton();
         this.regButton.$ = "extendedGUI.LoginWindow.GetReg";
         this.regButton.addEventListener(MouseEvent.CLICK,this.onRegHandler);
         this.regButton.setSize(200,30);
         this.regButton.size = 20;
         this.regButton.underline = false;
         this.regButton.autoWidth = true;
         this.regButton.tabEnabled = true;
         if(!Base.is_steam)
         {
            leftItems.addChild(this.regButton);
         }
      }
      
      protected function onRegHandler(event:Event) : void
      {
         Api.call(Api.OPEN_URL,[{"url":Locale.current.registrationUrl}]);
      }
      
      private function updateLoginButton() : void
      {
         this.loginButton.enabled = Boolean(Base.is_steam) || this.serverCombo.selectedIndex >= 0 && Boolean(this.loginInput.text.length) && Boolean(this.passwordInput.text.length);
      }
      
      override public function draw() : void
      {
         super.draw();
         this.serverBox.x = sideMargin;
         this.serverBox.setSize(width - sideMargin * 2,this.serverBox.height);
         this.serverCombo.listShift = 0;
         this.loginBox.x = sideMargin;
         this.loginBox.y = this.serverBox.y + this.serverBox.height;
         this.loginBox.setSize(width - sideMargin * 2,this.loginBox.height);
         this.passwordBox.x = sideMargin;
         this.passwordBox.y = this.loginBox.y + this.loginBox.height;
         this.passwordBox.setSize(width - sideMargin * 2,this.passwordBox.height);
         this.passwordInput.visible = Boolean(this.includeDeveloperServers) || !Base.is_steam;
         this.passwordLabel.visible = Boolean(this.includeDeveloperServers) || !Base.is_steam;
         this.loginInput.visible = Boolean(this.includeDeveloperServers) || !Base.is_steam;
         this.loginLabel.visible = Boolean(this.includeDeveloperServers) || !Base.is_steam;
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

