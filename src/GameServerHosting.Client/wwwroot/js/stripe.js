// Stripe Elements JavaScript Interop
window.stripeInterop = {
    stripe: null,
    elements: null,
    cardElement: null,

    // Initialize Stripe with publishable key
    initialize: function (publishableKey) {
        try {
            if (!window.Stripe) {
                console.error('Stripe.js has not loaded');
                return false;
            }
            this.stripe = window.Stripe(publishableKey);
            console.log('Stripe initialized successfully');
            return true;
        } catch (error) {
            console.error('Error initializing Stripe:', error);
            return false;
        }
    },

    // Create and mount card element
    createCardElement: function (elementId, style) {
        try {
            if (!this.stripe) {
                console.error('Stripe not initialized');
                return false;
            }

            // Create elements instance
            this.elements = this.stripe.elements();

            // Define default style if not provided
            const defaultStyle = style || {
                base: {
                    fontSize: '16px',
                    color: '#32325d',
                    fontFamily: '-apple-system, BlazorwebAssembly, "Segoe UI", Roboto, Oxygen, Ubuntu, Cantarell, "Helvetica Neue", sans-serif',
                    '::placeholder': {
                        color: '#aab7c4'
                    },
                    iconColor: '#666EE8'
                },
                invalid: {
                    color: '#fa755a',
                    iconColor: '#fa755a'
                }
            };

            // Create card element
            this.cardElement = this.elements.create('card', {
                style: defaultStyle,
                hidePostalCode: false
            });

            // Mount to DOM
            const container = document.getElementById(elementId);
            if (!container) {
                console.error('Card element container not found');
                return false;
            }

            this.cardElement.mount('#' + elementId);
            console.log('Card element mounted successfully');
            return true;
        } catch (error) {
            console.error('Error creating card element:', error);
            return false;
        }
    },

    // Add event listener for card changes
    addCardChangeListener: function (dotnetReference) {
        if (!this.cardElement) {
            console.error('Card element not created');
            return false;
        }

        this.cardElement.on('change', function (event) {
            dotnetReference.invokeMethodAsync('OnCardChange', {
                complete: event.complete,
                error: event.error ? event.error.message : null,
                brand: event.brand || 'unknown'
            });
        });

        return true;
    },

    // Create payment method from card element
    createPaymentMethod: async function (billingDetails) {
        try {
            if (!this.stripe || !this.cardElement) {
                return {
                    success: false,
                    error: 'Stripe or card element not initialized'
                };
            }

            const result = await this.stripe.createPaymentMethod({
                type: 'card',
                card: this.cardElement,
                billing_details: billingDetails
            });

            if (result.error) {
                return {
                    success: false,
                    error: result.error.message
                };
            }

            return {
                success: true,
                paymentMethodId: result.paymentMethod.id,
                brand: result.paymentMethod.card.brand,
                last4: result.paymentMethod.card.last4
            };
        } catch (error) {
            console.error('Error creating payment method:', error);
            return {
                success: false,
                error: error.message
            };
        }
    },

    // Confirm payment with payment intent
    confirmCardPayment: async function (clientSecret, paymentMethodId) {
        try {
            if (!this.stripe) {
                return {
                    success: false,
                    error: 'Stripe not initialized'
                };
            }

            const result = await this.stripe.confirmCardPayment(clientSecret, {
                payment_method: paymentMethodId
            });

            if (result.error) {
                return {
                    success: false,
                    error: result.error.message
                };
            }

            return {
                success: true,
                paymentIntentId: result.paymentIntent.id,
                status: result.paymentIntent.status
            };
        } catch (error) {
            console.error('Error confirming payment:', error);
            return {
                success: false,
                error: error.message
            };
        }
    },

    // Handle card action (3D Secure)
    handleCardAction: async function (clientSecret) {
        try {
            if (!this.stripe) {
                return {
                    success: false,
                    error: 'Stripe not initialized'
                };
            }

            const result = await this.stripe.handleCardAction(clientSecret);

            if (result.error) {
                return {
                    success: false,
                    error: result.error.message
                };
            }

            return {
                success: true,
                paymentIntentId: result.paymentIntent.id,
                status: result.paymentIntent.status
            };
        } catch (error) {
            console.error('Error handling card action:', error);
            return {
                success: false,
                error: error.message
            };
        }
    },

    // Destroy card element
    destroy: function () {
        try {
            if (this.cardElement) {
                this.cardElement.unmount();
                this.cardElement.destroy();
                this.cardElement = null;
            }
            if (this.elements) {
                this.elements = null;
            }
            console.log('Stripe elements destroyed');
            return true;
        } catch (error) {
            console.error('Error destroying elements:', error);
            return false;
        }
    },

    // Clear card element
    clear: function () {
        try {
            if (this.cardElement) {
                this.cardElement.clear();
                return true;
            }
            return false;
        } catch (error) {
            console.error('Error clearing card element:', error);
            return false;
        }
    }
};
