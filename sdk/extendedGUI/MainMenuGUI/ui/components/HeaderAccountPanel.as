package ui.components
{
   import com.dvalimona.components.*;
   import communication.*;
   import events.ApiEvent;
   import flash.display.*;
   import flash.events.*;
   import lang.*;
   
   public class HeaderAccountPanel extends Component
   {
      public var box:HBox;
      
      protected var accountLabel:PushButtonWithImage;
      
      protected var balanceLabel:PushButton;
      
      protected var balanceValue:PushButtonWithImage;
      
      protected var balanceButton:PushButtonWithImage;
      
      protected var shopButton:PushButtonWithImage;
      
      protected var storageButton:PushButtonWithImage;
      
      public function HeaderAccountPanel(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         this.visible = false;
         Gold.core.addEventListener(Gold.UPDATED,this.onGoldUpdated);
         Gold.core.addEventListener(Gold.UPDATED_PLATINUM,this.onGoldUpdated);
         Api.self.addEventListener(Api.STORAGE_NOTIFICATION_COUNT,this.onStorageNotification);
         Api.self.addEventListener(Api.SHOP_NOTIFICATION_COUNT,this.onShopNotification);
         Api.self.addEventListener(Api.SHOP_OPENED,this.onShopOpened);
         Api.self.addEventListener(Api.PARTNER_ID_CHANGE,this.onChangePartnerID);
         super(parent,xpos,ypos);
      }
      
      protected function onGoldUpdated(event:Event) : void
      {
         invalidate();
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         this.box = new HBox(this);
         this.box.spacing = 1;
         this.box.backgroundColor = 0;
         this.box.alignment = HBox.MIDDLE;
         this.box.horizontalAlign = HBox.RIGHT;
         this.box.backgroundAlpha = 0.2;
         this.box.spacing = 0;
         this.box.visible = true;
         this.storageButton = new PushButtonWithImage(this.box);
         this.storageButton.image = new Bitmap(new icon_shop(),"auto",true);
         this.storageButton.height = 50;
         this.storageButton.label = "СКЛАД";
         this.storageButton.labelOverColor = Style.GOLD_OVER;
         this.storageButton.paddingRight = 20;
         this.storageButton.autoWidth = true;
         this.storageButton.backgroundVisible = false;
         this.storageButton.align = Label.LEFT;
         this.storageButton.$ = "extendedGUI.GoldPanel.storage";
         this.storageButton.addEventListener(MouseEvent.CLICK,this.onStorageButtonClick);
         this.shopButton = new PushButtonWithImage(this.box);
         this.shopButton.image = new Bitmap(new icon_shop(),"auto",true);
         this.shopButton.height = 50;
         this.shopButton.labelOverColor = Style.GOLD_OVER;
         this.shopButton.label = "МАГАЗИН";
         this.shopButton.align = Label.LEFT;
         this.shopButton.autoWidth = true;
         this.shopButton.paddingRight = 30;
         this.shopButton.backgroundVisible = false;
         this.shopButton.$ = "extendedGUI.GoldPanel.shop";
         this.shopButton.addEventListener(MouseEvent.CLICK,this.onShopButtonClick);
         this.balanceButton = new PushButtonWithImage();
         this.balanceButton.labelOverColor = Style.GOLD_OVER;
         this.balanceButton.backgroundVisible = false;
         this.balanceButton.label = "+";
         this.balanceButton.$ = "extendedGUI.GoldPanel.buyGold";
         this.balanceButton.addEventListener(MouseEvent.CLICK,this.onBuyClick);
         this.balanceButton.autoWidth = true;
         this.balanceButton.align = Label.LEFT;
         this.balanceButton.size = 28;
         this.balanceButton.paddingRight = 30;
         this.balanceButton.paddingLeft = 0;
         this.balanceButton.visible = Base.gold_visible;
         this.balanceValue = new PushButtonWithImage(this.box);
         this.balanceValue.image = new Bitmap(new icon_goldcoin(),"auto",true);
         this.balanceValue.label = String(Gold.Value) + " " + Locale.getById("extendedGUI.GoldPanel.buyGold");
         this.balanceValue.$ = "extendedGUI.GoldPanel.balanceGold";
         this.balanceValue.height = 50;
         this.balanceValue.align = Label.LEFT;
         this.balanceValue.mouseEnabled = true;
         this.balanceValue.mouseChildren = true;
         this.balanceValue.backgroundVisible = false;
         this.balanceValue.autoWidth = true;
         this.balanceValue.labelOverColor = Style.GOLD_OVER;
         this.balanceValue.paddingRight = -20;
         this.balanceValue.downColorAlpha = 1;
         this.balanceValue.addEventListener(MouseEvent.CLICK,this.onBuyClick);
         this.balanceLabel = new PushButton(this.box);
         this.balanceLabel.autoWidth = true;
         this.balanceLabel.$ = "extendedGUI.GoldPanel.balanceGold";
         this.balanceLabel.height = 50;
         this.balanceLabel.mouseEnabled = false;
         this.balanceLabel.mouseChildren = false;
         this.balanceLabel.backgroundVisible = false;
         this.balanceLabel.autoWidth = true;
         this.balanceLabel.paddingLeft = 0;
         this.balanceLabel.paddingRight = 0;
         this.balanceLabel.visible = Base.gold_visible;
         this.balanceLabel.align = Label.LEFT;
         this.balanceLabel.addEventListener(MouseEvent.CLICK,this.onBuyClick);
         this.balanceLabel.upColorAlpha = 1;
         this.accountLabel = new PushButtonWithImage(this.box);
         if(Base.steam_trusted)
         {
            this.accountLabel.labelUpColor = Style.ACCOUNT_TRUSTED_LABEL;
            this.accountLabel.image = new Bitmap(new icon_user_trusted(),"auto",true);
            this.accountLabel.addEventListener(MouseEvent.MOUSE_OVER,this.onMouseOver);
         }
         else
         {
            this.accountLabel.labelUpColor = Style.BUTTON_UP_LABEL_COLOR;
            this.accountLabel.image = new Bitmap(new icon_user(),"auto",true);
            this.accountLabel.removeEventListener(MouseEvent.MOUSE_OVER,this.onMouseOver);
         }
         this.accountLabel.addEventListener(MouseEvent.MOUSE_OVER,this.onMouseOver);
         this.accountLabel.label = "Акаунт";
         this.accountLabel.visible = false;
         this.accountLabel.align = Label.LEFT;
         this.accountLabel.labelDownColor = Style.ACCOUNT_TRUSTED_LABEL;
         this.accountLabel.labelOverColor = Style.ACCOUNT_TRUSTED_LABEL;
         this.accountLabel.height = 50;
         this.accountLabel.mouseEnabled = Base.steam_trusted;
         this.accountLabel.mouseChildren = Base.steam_trusted;
         this.accountLabel.autoWidth = true;
         this.accountLabel.backgroundVisible = false;
         this.accountLabel.paddingRight = 30;
         invalidate();
      }
      
      public function onMouseOver(event:MouseEvent) : *
      {
         var str:String = Locale.getById("extendedGUI.ToolTips.accouunt_trusted");
         Base.navigator.showToolTip(event.target,str);
      }
      
      public function gold_visible_update() : void
      {
         this.balanceButton.visible = Base.gold_visible;
         this.balanceLabel.visible = Base.gold_visible;
         this.balanceValue.visible = Base.gold_visible;
         this.balanceButton.width = 0;
         this.balanceLabel.width = 0;
         this.balanceValue.width = 0;
         this.balanceButton.autoWidth = Base.gold_visible;
         this.balanceLabel.autoWidth = Base.gold_visible;
         this.balanceValue.autoWidth = Base.gold_visible;
         if(Base.gold_visible)
         {
            this.balanceValue.paddingRight = 30;
         }
         else
         {
            this.balanceValue.paddingRight = 0;
         }
      }
      
      public function set_trusted(value:Boolean) : void
      {
         if(value)
         {
            this.accountLabel.labelUpColor = Style.ACCOUNT_TRUSTED_LABEL;
            this.accountLabel.image = new Bitmap(new icon_user_trusted(),"auto",true);
            this.accountLabel.addEventListener(MouseEvent.MOUSE_OVER,this.onMouseOver);
         }
         else
         {
            this.accountLabel.labelUpColor = Style.BUTTON_UP_LABEL_COLOR;
            this.accountLabel.image = new Bitmap(new icon_user(),"auto",true);
            this.accountLabel.removeEventListener(MouseEvent.MOUSE_OVER,this.onMouseOver);
         }
         this.accountLabel.mouseEnabled = value;
         this.accountLabel.mouseChildren = value;
      }
      
      override public function set visible(value:Boolean) : void
      {
         super.visible = value;
         invalidate();
      }
      
      override public function set x(value:Number) : void
      {
         var tempError:Error = new Error();
         var stackTrace:String = tempError.getStackTrace();
         super.x = value;
      }
      
      protected function onBuyClick(event:MouseEvent) : void
      {
         Gold.addGoldOpenURL();
      }
      
      protected function onShopButtonClick(event:MouseEvent) : void
      {
         Base.SHOP_OPENING = true;
         Api.call(Api.OPEN_SHOP);
      }
      
      protected function onStorageButtonClick(event:MouseEvent) : void
      {
         Base.SHOP_OPENING = true;
         Api.call(Api.OPEN_STORAGE);
      }
      
      public function set accountName(value:String) : void
      {
         this.accountLabel.label = value;
         invalidate();
      }
      
      protected function onChangePartnerID(arg:ApiEvent) : *
      {
      }
      
      protected function onStorageNotification(arg1:ApiEvent) : void
      {
         var count:uint = 0;
         if(arg1.data.answer.count.toString() == "!")
         {
            this.storageButton.notifyText("!");
         }
         else
         {
            count = uint(arg1.data.answer.count);
            this.storageButton.notifyCount = count;
         }
      }
      
      protected function onShopNotification(arg1:ApiEvent) : void
      {
         var count:uint = 0;
         if(arg1.data.answer.count.toString() == "!")
         {
            this.shopButton.notifyText("!");
         }
         else
         {
            count = uint(arg1.data.answer.count);
            this.shopButton.notifyCount = count;
         }
      }
      
      protected function onShopOpened(arg1:ApiEvent) : void
      {
         Base.SHOP_OPENING = false;
      }
      
      public function get valueWidth() : Number
      {
         if(!Base.gold_visible)
         {
            return 0;
         }
         return 350 + this.balanceValue.get_label.textField.textWidth;
      }
      
      override public function draw() : void
      {
         super.draw();
         this.balanceValue.label = String(Gold.Value) + " " + Locale.getById("extendedGUI.GoldPanel.buyGold");
         this.balanceValue.invalidate();
         this.box.invalidate();
         this.gold_visible_update();
         this.balanceLabel.height = this.height;
         this.balanceValue.height = this.height;
         this.box.height = this.height;
         this.balanceButton.height = this.height;
         this.storageButton.draw();
         this.shopButton.draw();
         this.balanceButton.draw();
         this.balanceLabel.draw();
         this.accountLabel.draw();
      }
   }
}

