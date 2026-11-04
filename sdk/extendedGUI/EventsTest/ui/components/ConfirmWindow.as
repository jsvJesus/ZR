package ui.components
{
   import com.dvalimona.components.ClearButton;
   import com.dvalimona.components.InputText;
   import com.dvalimona.components.LabelShadowed;
   import com.dvalimona.components.PushButton;
   import com.dvalimona.components.Text;
   import com.dvalimona.components.VBox;
   import com.dvalimona.components.Window;
   import communication.Api;
   import communication.Auth;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.utils.setTimeout;
   import lang.Locale;
   import logging.Logger;
   import ui.Navigator;
   
   public class ConfirmWindow extends Window
   {
      public static var self:ConfirmWindow;
      
      public static const EMAIL:String = "email";
      
      public static const SMS:String = "sms";
      
      protected var count:uint;
      
      protected var deliveredBy:String;
      
      protected var msg:String;
      
      protected var vBox:VBox;
      
      protected var description:Text;
      
      protected var enterLine:HBoxLine;
      
      protected var enterLabel:LabelShadowed;
      
      protected var enterField:InputText;
      
      protected var enterButton:PushButton;
      
      protected var tryCountLabel:LabelShadowed;
      
      protected var quad:Quad;
      
      protected var refreshLabel:Text;
      
      protected var refreshButton:PushButton;
      
      protected var exitButton:PushButton;
      
      protected var allButtons:Array;
      
      public function ConfirmWindow(count:uint, deliveredBy:String, msg:String)
      {
         this.count = count;
         this.deliveredBy = deliveredBy;
         this.msg = msg;
         super();
      }
      
      public static function getInstance(count:uint, deliveredBy:String, msg:String) : ConfirmWindow
      {
         if(!self)
         {
            self = new ConfirmWindow(count,deliveredBy,msg);
         }
         else
         {
            self.count = count;
            self.deliveredBy = deliveredBy;
            self.msg = msg;
            self.enterField.text = "";
            self.enterField.enabled = true;
            self.enterButton.enabled = false;
         }
         self.invalidate();
         return self;
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         this.headerDeals();
         this.bodyDeals();
      }
      
      override protected function showOn() : void
      {
      }
      
      private function bodyDeals() : void
      {
         this.vBox = new VBox();
         this.vBox.x = 0;
         this.vBox.y = 0;
         this.vBox.alignment = VBox.JUSTIFY;
         this.vBox.spacing = 10;
         this.vBox.debug = false;
         this.addChild(this.vBox);
         this.description = new Text(this.vBox);
         this.description.size = 26;
         this.description.debug = false;
         this.description.editable = false;
         this.description.paddingTop = 20;
         this.description.paddingLeft = 20;
         this.description.paddingRight = 20;
         this.description.paddingBottom = 20;
         this.enterLine = new HBoxLine(this.vBox);
         this.enterLine.height = 50;
         this.enterLine.paddingTop = 0;
         this.enterLine.paddingLeft = 20;
         this.enterLine.paddingRight = 20;
         this.enterLine.paddingBottom = 0;
         this.enterLabel = new LabelShadowed(this.enterLine.left);
         this.enterLabel.size = 22;
         this.enterLabel.debug = false;
         this.enterButton = new PushButton(this.enterLine.right);
         this.enterButton.setSize(50,40);
         this.enterButton.addEventListener(MouseEvent.CLICK,this.onSendClick);
         this.enterButton.enabled = false;
         this.enterField = new InputText(this.enterLine.right);
         this.enterField.setSize(300,40);
         this.enterField.size = 22;
         this.enterField.addEventListener(Event.CHANGE,this.onFieldChanged);
         this.enterField.restrict = "A-Z 0-9 a-z _";
         this.tryCountLabel = new LabelShadowed(this.vBox);
         this.tryCountLabel.size = 18;
         this.tryCountLabel.color = 11776934;
         this.tryCountLabel.debug = false;
         this.tryCountLabel.paddingTop = 0;
         this.tryCountLabel.paddingLeft = 20;
         this.tryCountLabel.paddingRight = 20;
         this.tryCountLabel.paddingBottom = 0;
         this.quad = new Quad(this.vBox);
         this.quad.paddingTop = 20;
         this.quad.paddingLeft = 20;
         this.quad.paddingRight = 20;
         this.quad.paddingBottom = 20;
         this.refreshLabel = new Text(this.vBox);
         this.refreshLabel.size = 26;
         this.refreshLabel.paddingTop = 0;
         this.refreshLabel.paddingLeft = 20;
         this.refreshLabel.paddingRight = 20;
         this.refreshLabel.paddingBottom = 0;
         this.refreshLabel.editable = false;
         this.refreshButton = new PushButton(this.vBox);
         this.refreshButton.paddingTop = 20;
         this.refreshButton.paddingLeft = 20;
         this.refreshButton.paddingRight = 20;
         this.refreshButton.paddingBottom = 0;
         this.refreshButton.height = 50;
         this.refreshButton.addEventListener(MouseEvent.CLICK,this.onRefreshClick);
      }
      
      protected function onRefreshClick(event:MouseEvent) : void
      {
         this.refreshButton.enabled = false;
         Api.call(Api.CONFIRM_WINDOW_RESEND);
      }
      
      protected function onSendClick(event:MouseEvent) : void
      {
         this.enterButton.enabled = false;
         this.enterField.enabled = false;
         Api.call(Api.CONFIRM_WINDOW_OK,[{"key":this.enterField.text}]);
      }
      
      protected function onFieldChanged(event:Event) : void
      {
         this.enterButton.enabled = this.enterField.text.length > 0;
      }
      
      private function headerDeals() : void
      {
         leftItems.shift = 3;
         _titleLabel.autoSize = true;
         _titleLabel.font = Base.FONT_BOLD;
         _titleLabel.text = "Stalker Online GUARD";
         _titleLabel.size = 20;
         _titleLabel.mouseEnabled = false;
         _titleLabel.mouseChildren = false;
         this.exitButton = new ClearButton(this.rightItems);
         this.exitButton.autoWidth = true;
         this.exitButton.label = "выход";
         this.exitButton.addEventListener(MouseEvent.CLICK,this.onExitClick);
      }
      
      protected function onExitClick(event:MouseEvent) : void
      {
         this.doClose();
         Auth.self.doLogout();
      }
      
      public function doClose() : void
      {
         dispatchEvent(new Event(Event.CLOSE));
         this.close();
      }
      
      override protected function close() : void
      {
         Navigator.showOff([this,_titleBar,_titleLabel,_panel,this.description,this.enterLine,this.tryCountLabel,this.refreshLabel,this.refreshButton],this.onShowOffComplete);
      }
      
      private function onShowOffComplete() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onShowOffComplete");
         setTimeout(this.parent.removeChild,0,this);
         setTimeout(this.killMePlease,10,this);
      }
      
      private function killMePlease() : void
      {
         self = null;
      }
      
      override public function draw() : void
      {
         super.draw();
         this.vBox.setSize(this.width,1);
         switch(this.deliveredBy)
         {
            case EMAIL:
               this.description.text = Locale.getById("extendedGUI.ConfirmWindow.onEmail1") + " " + this.msg + " " + Locale.getById("extendedGUI.ConfirmWindow.onEmail2");
               break;
            case SMS:
            default:
               this.description.text = Locale.getById("extendedGUI.ConfirmWindow.onPhone1") + " " + this.msg + " " + Locale.getById("extendedGUI.ConfirmWindow.onPhone2");
         }
         this.enterLabel.text = Locale.getById("extendedGUI.ConfirmWindow.enterCode");
         this.enterButton.label = Locale.getById("extendedGUI.ConfirmWindow.okButton");
         this.tryCountLabel.text = Locale.getById("extendedGUI.ConfirmWindow.tryCounts") + " " + this.count;
         this.refreshLabel.text = Locale.getById("extendedGUI.ConfirmWindow.resendText");
         this.refreshButton.label = Locale.getById("extendedGUI.ConfirmWindow.resendButton");
         this.vBox.draw();
         Logger.LogToChannel(Logger.DEBUG,"ConfirmWindow",this.vBox.height,this.height);
         if(Math.round(this.vBox.height) != this.height)
         {
         }
      }
      
      private function adjust(... args) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"ConfirmWindow adjust",this.vBox.width,this.vBox.height);
      }
   }
}

