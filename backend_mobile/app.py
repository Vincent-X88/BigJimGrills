from flask import Flask, request, jsonify, send_from_directory
from flask_sqlalchemy import SQLAlchemy
from werkzeug.security import check_password_hash
from sqlalchemy.sql import text
import logging
from datetime import datetime
import random
import uuid
from sqlalchemy.exc import SQLAlchemyError


app = Flask(__name__)
#app.config['SQLALCHEMY_DATABASE_URI'] = "postgresql://postgres:fROPzOfugGItGZqKGxIrMoObeQasWFvb@junction.proxy.rlwy.net:19110/railway"
app.config['SQLALCHEMY_DATABASE_URI'] = "postgresql://postgres:xolani@localhost:5432/postgres"

app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False 

db = SQLAlchemy(app)

# Enable SQLAlchemy logging to see the executed SQL statements
logging.basicConfig()
logging.getLogger('sqlalchemy.engine').setLevel(logging.INFO)

class User(db.Model):
    id = db.Column(db.Integer, primary_key=True, autoincrement=True)
    first_name = db.Column(db.String(100))
    last_name = db.Column(db.String(100))
    email = db.Column(db.String(100), nullable=False, unique=True, index=True)
    password = db.Column(db.String(100), nullable=False)
    phone_number = db.Column(db.String(15), nullable=False)
    Role = db.Column(db.String(20), default='User')  
    IsRestricted = db.Column(db.Boolean, default=False) 


class MenuIngredient(db.Model):
    __tablename__ = 'menu_ingredients'
    id = db.Column(db.Integer, primary_key=True, autoincrement=True)
    name = db.Column(db.String(100), nullable=False) 
    category = db.Column(db.String(100), nullable=False) 
    price = db.Column(db.Float, nullable=False) 
    quantity = db.Column(db.Integer, nullable=False)


class FeaturedItem(db.Model):
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(100))
    description = db.Column(db.String(255))
    image_url = db.Column(db.String(255))
    price = db.Column(db.Float) 


class Cart(db.Model):
    id = db.Column(db.Integer, primary_key=True, autoincrement=True)
    user_id = db.Column(db.String(80), nullable=False)  
    product_id = db.Column(db.Integer, nullable=False)  
    quantity = db.Column(db.Integer, nullable=False)
    price = db.Column(db.Float, nullable=False)
    status = db.Column(db.String(50), nullable=False)  
    extra_ids = db.Column(db.String(255))  
    order_number = db.Column(db.String(20)) 
    scheduled_pickup_date = db.Column(db.Date)  
    scheduled_pickup_time = db.Column(db.Time)  
    is_scheduled = db.Column(db.Boolean, nullable=False, default=False) 
    created_at = db.Column(db.TIMESTAMP, nullable=False, default=datetime.utcnow)  
    special_note = db.Column(db.String(255))  
    

class Menus(db.Model):
    id = db.Column(db.Integer, primary_key=True) 
    name = db.Column(db.String(100))
    imageurl = db.Column(db.String(255), nullable=False)
    
class menu(db.Model):
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(80), nullable=False)
    category = db.Column(db.String(80), nullable=False)
    description = db.Column(db.String(255), nullable=False)
    imageurl = db.Column(db.String(255), nullable=False)
    price = db.Column(db.Float, nullable=False)

class Notifications(db.Model):
    id = db.Column(db.Integer, primary_key=True) 
    userid = db.Column(db.Integer, nullable=False)
    message = db.Column(db.String, nullable=False)
    createdat = db.Column(db.DateTime, nullable=False, default=datetime.utcnow)
    isread = db.Column(db.Boolean, nullable=False, default=False)
    orderid = db.Column(db.String, nullable=False)
    intended = db.Column(db.String)

class Receipts(db.Model):
    id = db.Column(db.Integer, primary_key=True)
    user_id = db.Column(db.String(80), nullable=False)
    total_price = db.Column(db.Float, nullable=False)
    items = db.Column(db.String(500), nullable=False)  # Store item names as comma-separated string

# Ensure we are running in the application context
with app.app_context():
    db.create_all()

@app.route('/register', methods=['POST'])
def register():
    data = request.get_json()
    if User.query.filter_by(email=data['email']).first():
        return jsonify({"message": "Email already exists"}), 400

    new_user = User(
        first_name=data['first_name'],
        last_name=data['last_name'],
        email=data['email'],
        password=data['password'],
        phone_number=data['phone_number'],
        Role = "User",  
        isRestricted = False
    )
    db.session.add(new_user)
    db.session.commit()

    return jsonify({"message": "User registered successfully"}), 201


@app.route('/login', methods=['POST'])
def login():
    data = request.get_json()
    user = User.query.filter_by(email=data['email']).first()

    if user and (user.password, data['password']):
        return jsonify({"message": "Login successful", "user_id": user.id}), 200
    else:
        return jsonify({"message": "Invalid email or password"}), 401
    


@app.route('/add_to_cart', methods=['POST'])
def add_to_cart():
    data = request.get_json()

    # Log received request data
    print(f'Received request data: %s', data)

    # Extract and validate fields
    user_id = data.get('userId')
    product_id = data.get('productId')
    quantity = data.get('quantity')
    price = data.get('price')
    extra_ids = data.get('extraIds', [])
    special_note = data.get('specialNote')
    is_schedule = data.get('isSchedule', False)
    scheduled_pickup_date = data.get('scheduledPickupDate')
    scheduled_pickup_time = data.get('scheduledPickupTime')

    # Validate required fields
    if not all([user_id, product_id, quantity is not None, price is not None]):
        missing_fields = {
            'userId': user_id,
            'productId': product_id,
            'quantity': quantity,
            'price': price
        }
        print(f'Missing or invalid fields: %s', missing_fields)
        return jsonify({
            'message': 'Invalid request payload',
            'missingData': missing_fields
        }), 400

    # Create a new cart item
    cart_item = Cart(
        user_id=user_id,
        product_id=product_id,
        quantity=quantity,
        price=price,
        extra_ids=extra_ids,
        special_note=special_note,
        is_schedule=is_schedule,
        scheduled_pickup_date=scheduled_pickup_date,
        scheduled_pickup_time=scheduled_pickup_time,
        status='pending',  # Set status to "Pending"
        created_at=datetime.utcnow()  # Set created_at to current time
    )

    # Attempt to add the item to the database
    try:
        db.session.add(cart_item)
        db.session.commit()
        logging.info('Item added to cart successfully: %s', cart_item)
        return jsonify({'message': 'Item added to cart'}), 200
    except Exception as e:
        db.session.rollback()
        logging.error('Error adding item to cart: %s', str(e))
        return jsonify({'message': f'Error: {str(e)}'}), 500


@app.route('/featured-items', methods=['GET'])
def get_featured_items():
    items = FeaturedItem.query.all()
    baseUrl = "http://192.168.43.36:5000/images/"
    
    result = []
    for item in items:
        image_url = baseUrl + item.image_url
        print(f"Image id for item {item.name}: {item.id}")
        result.append({
            "productId":item.id,
            "name": item.name,
            "description": item.description,
            "imageUrl": image_url,
            "price": item.price
        })
        
    return jsonify(result)


# Route to serve images from the 'images' directory
@app.route('/images/<path:filename>')
def serve_image(filename):
    return send_from_directory('images', filename)


@app.route('/cart', methods=['GET'])
def get_cart():
    user_id = request.args.get('userId')
    if not user_id:
        return jsonify({'message': 'User ID is required'}), 400

    try:
        # Query to fetch cart items and their details from the menu
        query_menu = text("""
        SELECT cart.id AS cart_id, cart.user_id AS cart_user_id, cart.product_id AS cart_product_id, cart.quantity AS cart_quantity, cart.price AS cart_price,
               menu.id AS menu_id, menu.name AS menu_name, menu.description AS menu_description,
               menu.imageurl AS menu_image_url, menu.price AS menu_price, menu.category AS menu_category
        FROM cart
        JOIN menu ON CAST(cart.product_id AS INTEGER) = menu.id
        WHERE cart.user_id = :user_id AND cart.status = 'pending'
        """)

        # Query to fetch cart items and their details from featured_item
        query_featured_item = text("""
        SELECT cart.id AS cart_id, cart.user_id AS cart_user_id, cart.product_id AS cart_product_id, cart.quantity AS cart_quantity, cart.price AS cart_price,
               featured_item.id AS featured_item_id, featured_item.name AS featured_item_name, featured_item.description AS featured_item_description,
               featured_item.image_url AS featured_item_image_url, featured_item.price AS featured_item_price
        FROM cart
        JOIN featured_item ON CAST(cart.product_id AS INTEGER) = featured_item.id
        WHERE cart.user_id = :user_id AND cart.status = 'pending'
        """)

        # Execute queries
        cart_items_menu = db.session.execute(query_menu, {'user_id': user_id}).fetchall()
        cart_items_featured = db.session.execute(query_featured_item, {'user_id': user_id}).fetchall()

        def get_base_url(category):
            category_base_urls = {
                'burgers': "http://192.168.43.36:5000/images/menus/burgers/",
                'drinks': "http://192.168.43.36:5000/images/menus/drinks/",
                'fries': "http://192.168.43.36:5000/images/menus/fries/",
                'pizzas': "http://192.168.43.36:5000/images/menus/pizzas/",
                'sandwiches': "http://192.168.43.36:5000/images/menus/sandwiches/"
            }
            return category_base_urls.get(category, "http://192.168.43.36:5000/images/menus/")

        items = []

        # Process menu items
        for item in cart_items_menu:
            items.append({
                'id': item.cart_id,
                'productId': item.cart_product_id,
                'name': item.menu_name,
                'imageUrl': get_base_url(item.menu_category) + item.menu_image_url,
                'quantity': item.cart_quantity,
                'price': item.cart_price
            })

        # Process featured items
        for item in cart_items_featured:
            items.append({
                'id': item.cart_id,
                'productId': item.cart_product_id,
                'name': item.featured_item_name,
                'imageUrl': "http://192.168.43.36:5000/images/" + item.featured_item_image_url,
                'quantity': item.cart_quantity,
                'price': item.cart_price
            })

        total_price = sum(item['price'] * item['quantity'] for item in items)
        formatted_total_price = round(total_price, 2)

        return jsonify({
            'items': items,
            'totalPrice': total_price
        }), 200

    except Exception as e:
        logging.error('Error fetching cart items: %s', str(e))
        return jsonify({'message': f'Error: {str(e)}'}), 500
    

@app.route('/cart/<int:item_id>', methods=['DELETE'])
def remove_from_cart(item_id):
    try:
        item = Cart.query.get(item_id)
        if not item:
            return jsonify({'message': 'Item not found'}), 404

        db.session.delete(item)
        db.session.commit()
        return jsonify({'message': 'Item removed from cart'}), 200
    except Exception as e:
        db.session.rollback()
        logging.error('Error removing item from cart: %s', str(e))
        return jsonify({'message': f'Error: {str(e)}'}), 500




@app.route('/categories', methods=['GET'])
def get_categories():
    baseUrl = "http://192.168.43.36:5000/images/menus/"
    try:
        
        categories = db.session.query(Menus).all()
        categories_list = [{
            'id': category.id,
            'name': category.name,
            'imageUrl': baseUrl + category.imageurl
        } for category in categories]
        return jsonify(categories_list), 200
    except Exception as e:
        logging.error('Error fetching categories: %s', str(e))
        return jsonify({'message': f'Error: {str(e)}'}), 500


@app.route('/menu_items', methods=['GET'])
def get_menu_items():
    category = request.args.get('category')
    print(f"Category received: {category}")
    
    if not category:
        return jsonify({'message': 'Category is required'}), 400

    try:
        # Fetch menu items by category
        items = menu.query.filter_by(category=category).all()
        print(f"Menu items fetched: {items}")

        baseUrl = "http://192.168.43.36:5000"

        # Define base URLs for different categories
        base_urls = {
            'burgers': baseUrl + "/images/menus/burgers/",
            'drinks': baseUrl + "/images/menus/drinks/",
            'fries': baseUrl + "/images/menus/fries/",
            'pizzas': baseUrl + "/images/menus/pizzas/",
            'sandwiches': baseUrl + "/images/menus/sandwiches/"
        }

        base_url = base_urls.get(category, baseUrl + "/images/menus/")
        print(f"Base URL selected: {base_url}")

        # Fetch ingredients based on the category
        ingredients = MenuIngredient.query.filter_by(category=category).all()
        print(f"Ingredients fetched for category '{category}': {ingredients}")

        result = [{
            'id': item.id,
            'name': item.name,
            'description': item.description,
            'imageUrl': f"{base_url}{item.imageurl}",
            'price': item.price,
            'ingredients': [
                {
                    'id': ingredient.id,
                    'name': ingredient.name,
                    'price': ingredient.price,
                    'quantity': ingredient.quantity
                }
                for ingredient in ingredients  # Ingredients are filtered by category
            ]
        } for item in items]

        print(f"Final result to return: {result}")

        return jsonify(result), 200

    except Exception as e:
        print(f"Error occurred: {str(e)}")
        return jsonify({'message': f'Error: {str(e)}'}), 500


@app.route('/get_profile/<int:user_id>', methods=['GET'])
def get_profile(user_id):
    user = User.query.get(user_id)
    print(user.first_name)
    if user:
        return jsonify({
            'firstName': user.first_name,
            'email': user.email,
            'phone': user.phone_number
        }), 200
     
    else:
        return jsonify({'message': 'User not found'}), 404


@app.route('/product/<int:id>', methods=['GET'])
def get_product_details(id):
    # Get the product by ID
    product = menu.query.filter_by(id=id).first()


    # Get extras based on product category
    extras = MenuIngredient.query.filter_by(category=product.category).all()

    # Check if all extras have quantity > 0 (availability)
    is_available = all(extra.quantity > 0 for extra in extras)

    # Prepare response
    product_details = {
        "id": product.id,
        "name": product.name,
        "description": product.description,
        "price": product.price,
        "category": product.category,
        "isAvailable": is_available,
        "extras": [
            {"id": extra.id, "name": extra.name, "quantity": extra.quantity} for extra in extras
        ]
    }

    return jsonify(product_details)


@app.route('/update_profile', methods=['PUT'])
def update_profile():
    data = request.get_json()
    user_id = data.get('userId')
    first_name = data.get('firstName')
    email = data.get('email')
    phone_number = data.get('phone')

    if not user_id or not first_name or not email or not phone_number:
        return jsonify({'message': 'Missing required fields'}), 400

    try:
        user = User.query.get(user_id)
        if not user:
            return jsonify({'message': 'User not found'}), 404

        # Update user details
        user.first_name = first_name
        user.email = email
        user.phone_number = phone_number

        db.session.commit()

        return jsonify({'message': 'Profile updated successfully'}), 200

    except Exception as e:
        db.session.rollback()
        logging.error('Error updating profile: %s', str(e))
        return jsonify({'message': f'Error: {str(e)}'}), 500



@app.route('/complete_checkout', methods=['POST'])
def complete_checkout():
    try:
        data = request.json
        user_id = data.get('userId')

        if not user_id:
            return jsonify({'message': 'User ID is required'}), 400

        # Fetch the user's name 
        user = User.query.get(user_id)
        user_name = user.first_name if user else "Customer"

        # Fetch cart items for the user
        cart_items = Cart.query.filter_by(user_id=user_id, status='pending').all()

        if not cart_items:
            return jsonify({'message': 'No items in cart'}), 400

        # Generate a unique order number
        order_number = str(uuid.uuid4())[:8].upper()

        # Update cart items to 'paid' status and assign order number
        for item in cart_items:
            item.status = 'Order Approved'
            item.order_number = order_number

        # Commit changes for cart items
        db.session.commit()

        # Create and save a notification
        notification_message = f"A new order (Order No: {order_number}) has been placed with {len(cart_items)} items."
        notification = Notifications(userid=user_id, message=notification_message, orderid=order_number, intended="admin", createdat=datetime.utcnow(), isread=False)
        db.session.add(notification)
        db.session.commit()

        print(f"Order Number: {order_number}, User Name: {user_name}")

        # Return the order number and user name
        return jsonify({
            'orderNumber': order_number,
            'userName': user_name
        })

    except Exception as e:
        return jsonify({'message': 'An unexpected error occurred.'}), 500

@app.route('/notifications', methods=['GET'])
def get_notifications():
    user_id = request.args.get('userId')

    if not user_id:
        return jsonify({'message': 'User ID is required'}), 400

    notifications = Notifications.query.filter_by(
        userid=user_id,
        isread=False,
        intended='customer'
    ).all()

    notification_list = [
        {
            'id': notification.id,
            'message': notification.message,
            'created_at': notification.createdat,
        } for notification in notifications
    ]

    return jsonify(notification_list)

@app.route('/notification/count', methods=['GET'])
def get_notification_count():
    user_id = request.args.get('user_id')

    if not user_id:
        return jsonify({'message': 'User ID is required'}), 400

    count = Notifications.query.filter_by(
        user_id=user_id,
        is_read=False,
        intended='customer'
    ).count()

    return jsonify({'notification_count': count})

@app.route('/notifications/clear', methods=['POST'])
def clear_notifications():
    user_id = request.json.get('user_id')

    if not user_id:
        return jsonify({'message': 'User ID is required'}), 400

    notifications = Notifications.query.filter_by(
        user_id=user_id,
        is_read=False,
        intended='customer'
    ).all()

    for notification in notifications:
        notification.is_read = True

    db.session.commit()

    return jsonify({'message': 'Notifications cleared successfully'})


@app.route('/menu_item/<int:product_id>', methods=['GET'])
def get_menu_item_by_id(product_id):
    try:
        print(f"Fetching product with ID: {product_id}")
        
        # Fetch the product by ID
        item = menu.query.filter_by(id=product_id).first()
        if not item:
            print(f"Product with ID {product_id} not found.")
            return jsonify({'message': 'Product not found'}), 404

        print(f"Product fetched: {item}")

        # Fetch category from the product
        category = item.category
        print(f"Product category: {category}")

        baseUrl = "http://192.168.43.36:5000"
        
        # Define base URLs for images based on category
        base_urls = {
            'burgers': baseUrl + "/images/menus/burgers/",
            'drinks': baseUrl + "/images/menus/drinks/",
            'fries': baseUrl + "/images/menus/fries/",
            'pizzas': baseUrl + "/images/menus/pizzas/",
            'sandwiches': baseUrl + "/images/menus/sandwiches/"
        }

        base_url = base_urls.get(category, baseUrl + "/images/menus/")
        print(f"Base URL selected: {base_url}")

        # Fetch ingredients by category
        ingredients = MenuIngredient.query.filter_by(category=category).all()
        print(f"Ingredients for category '{category}': {ingredients}")

        result = {
            'id': item.id,
            'name': item.name,
            'description': item.description,
            'imageUrl': f"{base_url}{item.imageurl}",
            'price': item.price,
            'ingredients': [
                {
                    'id': ingredient.id,
                    'name': ingredient.name,
                    'price': ingredient.price,
                    'quantity': ingredient.quantity
                }
                for ingredient in ingredients
            ]
        }

        print(f"Final result: {result}")

        return jsonify(result), 200

    except Exception as e:
        print(f"Error occurred: {str(e)}")
        return jsonify({'message': f'Error: {str(e)}'}), 500

if __name__ == '__main__':
    app.run(host='192.168.43.36', port=5000, debug=True)
